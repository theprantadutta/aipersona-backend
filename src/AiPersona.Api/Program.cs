using System.Text;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using Serilog;
using AiPersona.Api.Hubs;
using AiPersona.Api.Services;
using AiPersona.Application;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Infrastructure;

// Load .env file if exists
var envPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
if (File.Exists(envPath))
{
    foreach (var line in File.ReadAllLines(envPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.StartsWith('#'))
            continue;

        var parts = line.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 2)
        {
            Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/aipersona-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add Application and Infrastructure services
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Add HttpContextAccessor for CurrentUserService
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// Configure JWT Authentication
var jwtSecret = builder.Configuration["Jwt:SecretKey"]
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? throw new InvalidOperationException("JWT secret key is not configured");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? Environment.GetEnvironmentVariable("JWT_ISSUER")
    ?? "AiPersona";

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? Environment.GetEnvironmentVariable("JWT_AUDIENCE")
    ?? "AiPersonaApp";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero
    };

    // Support SignalR token from query string
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

// Add Controllers with JSON options
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower;
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

// Add API versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
});

// Add OpenAPI / Swagger
builder.Services.AddOpenApi();

// Add SignalR
builder.Services.AddSignalR();

// Configure CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000", "http://localhost:5173"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// Map SignalR Hubs
app.MapHub<ChatHub>("/hubs/chat");
app.MapHub<NotificationHub>("/hubs/notifications");

// Configure Hangfire Dashboard (only in development or with auth)
if (app.Environment.IsDevelopment())
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireLocalAuthFilter()],
        DashboardTitle = "AiPersona Background Jobs"
    });
}
else
{
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = [new HangfireAdminAuthFilter()],
        DashboardTitle = "AiPersona Background Jobs"
    });
}

// Schedule recurring jobs
RecurringJob.AddOrUpdate<AiPersona.Infrastructure.Jobs.CleanupFreeTierHistoryJob>(
    "cleanup-free-tier-history",
    job => job.Execute(),
    "0 0 * * *"); // Daily at midnight UTC

RecurringJob.AddOrUpdate<AiPersona.Infrastructure.Jobs.ResetDailyCountersJob>(
    "reset-daily-counters",
    job => job.Execute(),
    "0 0 * * *"); // Daily at midnight UTC

RecurringJob.AddOrUpdate<AiPersona.Infrastructure.Jobs.CheckSubscriptionExpirationsJob>(
    "check-subscription-expirations",
    job => job.Execute(),
    "30 0 * * *"); // Daily at 00:30 UTC

app.Run();

// Hangfire Auth Filters
public class HangfireLocalAuthFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}

public class HangfireAdminAuthFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context)
    {
        // In production, require authenticated admin user via the JWT token
        // The user must have accessed the dashboard through an authenticated endpoint
        // For now, allow local requests only in production
        return false; // Disable in production by default - enable via authenticated proxy
    }
}
