using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RealEstateTax.Application.Common.Interfaces;
using RealEstateTax.Application.Services;
using RealEstateTax.Domain.Services;
using RealEstateTax.Infrastructure.Identity;
using RealEstateTax.Infrastructure.Persistence;
using RealEstateTax.Infrastructure.BackgroundJobs;
using RealEstateTax.Infrastructure.Services;
using RealEstateTax.Infrastructure.Services.ApplicationServices;

namespace RealEstateTax.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // EF Core with PostGIS
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.UseNetTopologySuite();
                npgsql.EnableRetryOnFailure(3);
            }));

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        // JWT Authentication
        var jwtSecret = configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        // Hangfire
        services.AddHangfire(config =>
            config.UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(connectionString)));
        services.AddHangfireServer();

        // Background job classes (resolved by Hangfire's IoC activator)
        services.AddScoped<IntegrationProcessingJob>();
        services.AddScoped<BillReminderJob>();
        services.AddScoped<PenaltyCalculationJob>();
        services.AddScoped<RiskRecalculationJob>();

        // Infrastructure services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Domain services
        services.AddScoped<IPropertyMatchingService, PropertyMatchingService>();
        services.AddScoped<ITaxCalculationService, TaxCalculationService>();
        services.AddScoped<IValuationService, ValuationDomainService>();
        services.AddScoped<IRiskScoringService, RiskScoringService>();
        services.AddScoped<IExemptionService, ExemptionDomainService>();

        // Application services
        services.AddScoped<IAuthService, AuthAppService>();
        services.AddScoped<ITaxpayerService, TaxpayerAppService>();
        services.AddScoped<IPropertyService, PropertyAppService>();
        services.AddScoped<IEnumerationService, EnumerationAppService>();
        services.AddScoped<IValuationAppService, ValuationApplicationService>();
        services.AddScoped<ITaxAssessmentAppService, TaxAssessmentAppService>();
        services.AddScoped<IBillService, BillAppService>();
        services.AddScoped<IPaymentService, PaymentAppService>();
        services.AddScoped<IAppealService, AppealAppService>();
        services.AddScoped<IExemptionAppService, ExemptionApplicationService>();
        services.AddScoped<IRiskAppService, RiskApplicationService>();
        services.AddScoped<IIntegrationService, IntegrationAppService>();
        services.AddScoped<IAdminService, AdminAppService>();

        return services;
    }
}
