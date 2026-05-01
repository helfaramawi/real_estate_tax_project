using AspNetCoreRateLimit;
using FluentValidation.AspNetCore;
using Hangfire;
using Microsoft.AspNetCore.Http.Features;
using RealEstateTax.Infrastructure.BackgroundJobs;
using Microsoft.OpenApi.Models;
using RealEstateTax.API.Middleware;
using RealEstateTax.Application;
using RealEstateTax.Infrastructure;
using RealEstateTax.Infrastructure.Persistence;
using RealEstateTax.Intelligence;
using Serilog;
using Serilog.Events;

// ─── Bootstrap Serilog ────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog from config ──────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, lc) => lc
        .ReadFrom.Configuration(ctx.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    // ─── Layers DI ───────────────────────────────────────────────────────────
    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
    builder.Services.AddIntelligence(builder.Configuration, builder.Environment);

    // ─── HTTP context ─────────────────────────────────────────────────────────
    builder.Services.AddHttpContextAccessor();

    // ─── Controllers (includes Intelligence assembly for /api/v2/* routes) ──
    builder.Services.AddControllers()
        .AddApplicationPart(typeof(RealEstateTax.Intelligence.API.Controllers.IntelligenceController).Assembly)
        .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase)
        .AddFluentValidation();
    builder.Services.AddFluentValidationAutoValidation();

    // ─── Swagger / OpenAPI ────────────────────────────────────────────────────
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Real Estate Tax Intelligence Platform – Egypt",
            Version = "v1",
            Description = "Backend API for the Egyptian Real Estate Tax Administration System",
            Contact = new OpenApiContact { Name = "Tax Authority IT", Email = "it@retax.gov.eg" }
        });

        var jwtScheme = new OpenApiSecurityScheme
        {
            BearerFormat = "JWT",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            Description = "Enter: Bearer {token}",
            Reference = new OpenApiReference { Id = "Bearer", Type = ReferenceType.SecurityScheme }
        };
        c.AddSecurityDefinition("Bearer", jwtScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement { { jwtScheme, Array.Empty<string>() } });

        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
    });

    // ─── Rate limiting ────────────────────────────────────────────────────────
    builder.Services.AddMemoryCache();
    builder.Services.Configure<IpRateLimitOptions>(builder.Configuration.GetSection("IpRateLimiting"));
    builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
    builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
    builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
    builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
    builder.Services.AddInMemoryRateLimiting();

    // ─── File upload size limit ───────────────────────────────────────────────
    builder.Services.Configure<FormOptions>(o => o.MultipartBodyLengthLimit = 20 * 1024 * 1024);

    // ─── CORS ─────────────────────────────────────────────────────────────────
    builder.Services.AddCors(o => o.AddPolicy("DefaultPolicy", p =>
        p.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:3000"])
         .AllowAnyMethod()
         .AllowAnyHeader()
         .AllowCredentials()));

    // ─── Health checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ─── Seed reference data (schema created by V1__InitialSchema.sql) ────────
    try
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await ApplicationDbContextSeed.SeedAsync(db);
    }
    catch (Exception ex)
    {
        Log.Error(ex, "Seeding failed — inner: {Inner}", ex.InnerException?.Message ?? "none");
        // Log full chain
        var inner = ex.InnerException;
        while (inner != null)
        {
            Log.Error("  Caused by: {Type}: {Msg}", inner.GetType().Name, inner.Message);
            inner = inner.InnerException;
        }
        // Do not crash — the API can start without seed data
    }

    // ─── Middleware pipeline ──────────────────────────────────────────────────
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseSerilogRequestLogging(o =>
    {
        o.MessageTemplate = "{RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms | CorrelationId: {CorrelationId}";
        o.EnrichDiagnosticContext = (dc, ctx) =>
        {
            dc.Set("CorrelationId", ctx.Request.Headers["X-Correlation-ID"].FirstOrDefault() ?? ctx.TraceIdentifier);
            dc.Set("UserAgent", ctx.Request.Headers.UserAgent.ToString());
        };
    });

    app.UseIpRateLimiting();
    app.UseCors("DefaultPolicy");

    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "RET Platform v1");
        c.RoutePrefix = "swagger";
    });

    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health");
    app.UseHangfireDashboard("/hangfire", new DashboardOptions
    {
        Authorization = new[] { new HangfireAllowAllFilter() },
        DashboardTitle = "ReTax — Background Jobs",
    });

    // ─── Recurring jobs ───────────────────────────────────────────────────────
    RecurringJob.AddOrUpdate<BillReminderJob>(
        "bill-reminders-daily",
        j => j.SendRemindersAsync(CancellationToken.None),
        Cron.Daily(8));   // 08:00 UTC every day

    RecurringJob.AddOrUpdate<BillReminderJob>(
        "mark-overdue-bills-daily",
        j => j.MarkOverdueBillsAsync(CancellationToken.None),
        Cron.Daily(0));   // midnight UTC every day

    RecurringJob.AddOrUpdate<PenaltyCalculationJob>(
        "penalty-calculation-monthly",
        j => j.CalculateAndApplyPenaltiesAsync(CancellationToken.None),
        Cron.Monthly(1, 6));  // 1st of each month at 06:00 UTC

    // ─── Intelligence recurring jobs (all gated by feature flags in jobs) ────
    IntelligenceDependencyInjection.RegisterIntelligenceRecurringJobs();

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }

internal sealed class HangfireAllowAllFilter : Hangfire.Dashboard.IDashboardAuthorizationFilter
{
    public bool Authorize(Hangfire.Dashboard.DashboardContext context) => true;
}
