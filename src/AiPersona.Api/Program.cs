using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Hangfire;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using Serilog.Events;
using AiPersona.Api.Hubs;
using AiPersona.Api.Services;
using AiPersona.Application;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Infrastructure;

// Load .env file if it exists (check multiple locations)
var possibleEnvPaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", ".env"),
    Path.Combine(AppContext.BaseDirectory, ".env"),
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", ".env"),
};

var envFilePath = possibleEnvPaths.FirstOrDefault(File.Exists);
if (envFilePath != null)
{
    Console.WriteLine($"[ENV] Loading environment from: {Path.GetFullPath(envFilePath)}");
    foreach (var line in File.ReadAllLines(envFilePath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            continue;

        var parts = line.Split('=', 2);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}
else
{
    Console.WriteLine("[ENV] WARNING: No .env file found! Checked paths:");
    foreach (var p in possibleEnvPaths)
        Console.WriteLine($"  - {Path.GetFullPath(p)}");
}

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Hangfire", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithThreadId()
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/aipersona-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Starting AiPersona API...");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog();

    // Add Application and Infrastructure layers
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);

    // Add HttpContextAccessor for CurrentUserService
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Configure JSON serialization (snake_case for frontend compatibility)
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

    // Configure CORS
    var allowedOrigins = builder.Configuration["AllowedOrigins"]
        ?? Environment.GetEnvironmentVariable("ALLOWED_ORIGINS")
        ?? "*";

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (allowedOrigins == "*")
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            }
            else
            {
                policy.WithOrigins(allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
        });
    });

    // Configure JWT Authentication
    var jwtSecretKey = !string.IsNullOrEmpty(builder.Configuration["Jwt:SecretKey"])
        ? builder.Configuration["Jwt:SecretKey"]!
        : Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
          ?? throw new InvalidOperationException("JWT_SECRET_KEY is not configured");

    var jwtIssuer = !string.IsNullOrEmpty(builder.Configuration["Jwt:Issuer"])
        ? builder.Configuration["Jwt:Issuer"]!
        : Environment.GetEnvironmentVariable("JWT_ISSUER") ?? "AiPersona";

    var jwtAudience = !string.IsNullOrEmpty(builder.Configuration["Jwt:Audience"])
        ? builder.Configuration["Jwt:Audience"]!
        : Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "AiPersonaApp";

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Support SignalR authentication via query string
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

    builder.Services.AddAuthorization();

    // Add SignalR
    builder.Services.AddSignalR()
        .AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
        });

    // Configure OpenAPI with Scalar
    builder.Services.AddOpenApi();

    var app = builder.Build();

    // Configure the HTTP request pipeline

    // Global exception handler
    app.UseExceptionHandler(errorApp =>
    {
        errorApp.Run(async context =>
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
            if (error != null)
            {
                Log.Error(error.Error, "Unhandled exception");

                var response = new
                {
                    error = "An unexpected error occurred",
                    message = app.Environment.IsDevelopment() ? error.Error.Message : "Internal server error"
                };

                await context.Response.WriteAsJsonAsync(response);
            }
        });
    });

    // Request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
    });

    app.UseHttpsRedirection();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();

    // OpenAPI and Scalar UI (only in Development)
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
        app.MapScalarApiReference(options =>
        {
            options.WithTheme(ScalarTheme.Mars).WithTitle("AiPersona API");
        });
    }

    // Hangfire Dashboard
    var hangfireUsername = Environment.GetEnvironmentVariable("HANGFIRE_USERNAME") ?? "admin";
    var hangfirePassword = Environment.GetEnvironmentVariable("HANGFIRE_PASSWORD") ?? "admin";

    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAuthorizationFilter(
            app.Environment.IsDevelopment(),
            hangfireUsername,
            hangfirePassword) }
    });

    // Configure Hangfire recurring jobs
    ConfigureRecurringJobs();

    // Map controllers and SignalR hubs
    app.MapControllers();
    app.MapHub<ChatHub>("/hubs/chat");
    app.MapHub<NotificationHub>("/hubs/notifications");

    // Health check endpoint
    app.MapGet("/health", () => Results.Ok(new
    {
        status = "healthy",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    }));

    // Root endpoint
    app.MapGet("/", (IWebHostEnvironment env) => env.IsDevelopment()
        ? Results.Redirect("/scalar/v1")
        : Results.Ok(new { name = "AiPersona API", version = "1.0.0", status = "running" }));

    var port = Environment.GetEnvironmentVariable("API_PORT") ?? "8001";
    app.Urls.Add($"http://0.0.0.0:{port}");

    // Check for --seed-personas CLI argument or SEED_PERSONAS env var
    var shouldSeedPersonas = args.Contains("--seed-personas") ||
                              Environment.GetEnvironmentVariable("SEED_PERSONAS") == "true";

    if (shouldSeedPersonas)
    {
        Log.Information("Running persona seeder...");
        using var scope = app.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<AiPersona.Infrastructure.Services.IPersonaSeederService>();

        try
        {
            var result = await seeder.SeedPersonasAsync();
            Log.Information("Seeding completed: {Created} created, {Updated} updated, {Skipped} skipped, {Errors} errors",
                result.Created, result.Updated, result.Skipped, result.Errors.Count);

            if (result.Errors.Count > 0)
            {
                foreach (var error in result.Errors)
                {
                    Log.Warning("Seeding error: {Error}", error);
                }
            }

            // Exit after seeding if only seeding was requested
            if (args.Contains("--seed-only"))
            {
                Log.Information("Seed-only mode, exiting...");
                return;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to seed personas");
            if (args.Contains("--seed-only"))
            {
                throw;
            }
        }
    }

    Log.Information("AiPersona API started on port {Port}", port);

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Configure recurring background jobs
static void ConfigureRecurringJobs()
{
    // Cleanup free tier history - daily at midnight UTC
    RecurringJob.AddOrUpdate<AiPersona.Infrastructure.Jobs.CleanupFreeTierHistoryJob>(
        "cleanup-free-tier-history",
        job => job.Execute(),
        "0 0 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // Reset daily counters - daily at midnight UTC
    RecurringJob.AddOrUpdate<AiPersona.Infrastructure.Jobs.ResetDailyCountersJob>(
        "reset-daily-counters",
        job => job.Execute(),
        "0 0 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    // Check subscription expirations - daily at 00:30 UTC
    RecurringJob.AddOrUpdate<AiPersona.Infrastructure.Jobs.CheckSubscriptionExpirationsJob>(
        "check-subscription-expirations",
        job => job.Execute(),
        "30 0 * * *",
        new RecurringJobOptions { TimeZone = TimeZoneInfo.Utc });

    Log.Information("Hangfire recurring jobs configured successfully");
}

// Hangfire authorization filter for dashboard with Basic Auth
public class HangfireAuthorizationFilter : IDashboardAuthorizationFilter
{
    private readonly bool _isDevelopment;
    private readonly string _username;
    private readonly string _password;

    public HangfireAuthorizationFilter(bool isDevelopment, string username, string password)
    {
        _isDevelopment = isDevelopment;
        _username = username;
        _password = password;
    }

    public bool Authorize(DashboardContext context)
    {
        // In development, allow all access without auth
        if (_isDevelopment)
            return true;

        // In production, require Basic Auth
        var httpContext = context.GetHttpContext();
        var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
        {
            SetUnauthorizedResponse(httpContext);
            return false;
        }

        try
        {
            var encodedCredentials = authHeader.Substring("Basic ".Length).Trim();
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var parts = credentials.Split(':', 2);

            if (parts.Length == 2 && parts[0] == _username && parts[1] == _password)
                return true;
        }
        catch
        {
            // Invalid base64 or other error
        }

        SetUnauthorizedResponse(httpContext);
        return false;
    }

    private static void SetUnauthorizedResponse(HttpContext httpContext)
    {
        httpContext.Response.StatusCode = 401;
        httpContext.Response.Headers["WWW-Authenticate"] = "Basic realm=\"Hangfire Dashboard\"";
    }
}
