using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using AiPersona.Application.Common.Interfaces;
using AiPersona.Infrastructure.Persistence;
using AiPersona.Infrastructure.Services;

namespace AiPersona.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database connection string from environment or config
        var connectionString = BuildConnectionString(configuration);

        // Entity Framework Core with PostgreSQL
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "public");
                npgsqlOptions.EnableRetryOnFailure(3);
            });
        });

        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Hangfire with PostgreSQL
        var hangfireSchema = configuration["Hangfire:SchemaName"]
            ?? Environment.GetEnvironmentVariable("HANGFIRE_SCHEMA")
            ?? "aipersona_hangfire";

        services.AddHangfire(config =>
        {
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180);
            config.UseSimpleAssemblyNameTypeSerializer();
            config.UseRecommendedSerializerSettings();
            config.UsePostgreSqlStorage(options =>
            {
                options.UseNpgsqlConnection(connectionString);
            }, new PostgreSqlStorageOptions
            {
                SchemaName = hangfireSchema,
                QueuePollInterval = TimeSpan.FromSeconds(15),
                PrepareSchemaIfNecessary = true
            });
        });

        services.AddHangfireServer();

        // HTTP client for external APIs
        services.AddHttpClient<IGeminiService, GeminiService>();
        services.AddHttpClient<IFileService, FileService>();

        // Firebase services
        services.AddSingleton<IFirebaseAuthService, FirebaseAuthService>();
        services.AddSingleton<IFcmService, FcmService>();

        // JWT service
        services.AddSingleton<IJwtService, JwtService>();

        // DateTime service
        services.AddSingleton<IDateTimeService, DateTimeService>();

        // Password hasher
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Google Play service
        services.AddSingleton<IGooglePlayService, GooglePlayService>();

        // FileRunner service
        services.AddHttpClient<IFileRunnerService, FileRunnerService>();

        // Persona seeder service
        services.AddScoped<IPersonaSeederService, PersonaSeederService>();

        return services;
    }

    private static string BuildConnectionString(IConfiguration configuration)
    {
        // Try to get full connection string first
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (!string.IsNullOrEmpty(connectionString))
        {
            return connectionString;
        }

        // Build from environment variables
        var host = Environment.GetEnvironmentVariable("DATABASE_HOST") ?? "localhost";
        var port = Environment.GetEnvironmentVariable("DATABASE_PORT") ?? "5432";
        var database = Environment.GetEnvironmentVariable("DATABASE_NAME") ?? "aipersona";
        var username = Environment.GetEnvironmentVariable("DATABASE_USERNAME") ?? "postgres";
        var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD") ?? "";

        return $"Host={host};Port={port};Database={database};Username={username};Password={password}";
    }
}
