using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Joby.Application.Interfaces;
using Joby.Infrastructure.BackgroundJobs;
using Joby.Infrastructure.Email;
using Joby.Infrastructure.Persistence;
using Joby.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Joby.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection")));

        // Hangfire
        services.AddHangfire(config =>
            config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings()
                .UsePostgreSqlStorage(options =>
                    options.UseNpgsqlConnection(configuration.GetConnectionString("DefaultConnection"))));

        services.AddHangfireServer();

        // JWT Authentication
        var jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("JWT secret not configured");
        var key = Encoding.UTF8.GetBytes(jwtSecret);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // HTTP Client
        services.AddHttpClient<IJobScraper, JobScraper>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        // Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IReminderService, ReminderService>();
        services.AddScoped<IRecommendationService, RecommendationService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IResumeParser, ResumeParser>();

        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.AddSingleton<IEmailSender, SmtpEmailSender>();

        // Background Jobs
        services.AddScoped<RecommendationRefreshJob>();
        services.AddScoped<ReminderEmailDispatchJob>();

        return services;
    }

    public static void ConfigureHangfireJobs(this IServiceProvider serviceProvider)
    {
        // IMPORTANT: Use the service-based API (IRecurringJobManager) instead of the static RecurringJob API.
        // The static API relies on JobStorage.Current which may not be initialized yet in some hosting environments (e.g. Kubernetes).
        var recurringJobManager = serviceProvider.GetRequiredService<IRecurringJobManager>();

        // Schedule recurring job to refresh recommendations every 6 hours
        recurringJobManager.AddOrUpdate<RecommendationRefreshJob>(
            "refresh-recommendations",
            job => job.ExecuteAsync(),
            "0 */6 * * *"); // Every 6 hours

        recurringJobManager.AddOrUpdate<ReminderEmailDispatchJob>(
            "dispatch-reminder-emails",
            job => job.ExecuteAsync(),
            "* * * * *"); // Every minute
    }
}


