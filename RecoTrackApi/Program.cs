using Hangfire;
using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using RecoTrack.Application.Services;
using RecoTrack.Data.Repositories;
using RecoTrack.Infrastructure.Services;
using RecoTrack.Shared.Settings;
using RecoTrackApi.Configurations;
using RecoTrackApi.CustomMiddlewares;
using RecoTrackApi.Extensions;
using RecoTrackApi.Jobs;
using RecoTrackApi.Repositories;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;
using RecoTrackApi.Services.Interfaces;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//Serilog Bootstrap Logger
//(Logs to console until DI container is built)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

//Configuration
var configuration = builder.Configuration;

//mongo configurations
builder.Services.AddMongo(builder.Configuration);

var jwtKey = configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT secret key is not configured");


//Health Checks
builder.Services.AddHealthChecks();

//Dependency Injection: Repositories & Services
builder.Services.AddSingleton<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddSingleton<RecoTrackApi.Repositories.Interfaces.ILogRepository, RecoTrackApi.Repositories.LogRepository>();
builder.Services.AddScoped<RecoTrack.Application.Interfaces.ILogRepository, RecoTrack.Infrastructure.Services.LogRepository>();
builder.Services.AddScoped<IJobMetricsRepository, JobMetricsRepository>();
builder.Services.AddScoped<ILogCleanerService, LogCleanerService>();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();

//HTTP Clients
builder.Services.AddHttpClient<IAutomatedPrReviewService, AutomatedPrReviewService>();
builder.Services.AddHttpClient<IGitHubClientService, GitHubClientService>();
builder.Services.AddScoped<IAutomatedPrReviewService, AutomatedPrReviewService>();

// Job registration
builder.Services.AddScoped<IEmailJob, EmailJob>();
//emailserviceHangfire
builder.Services.AddScoped<IEmailAuditRepository, EmailAuditRepository>();

builder.Services.Configure<SmtpOptions>(configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailSender, MailKitEmailSender>();

//custom Extension method for swagger 
builder.Services.AddSwaggerDocumentation();

//Hangfire Setup
var hangfireOptions = new MongoStorageOptions
{
    Prefix = "hangfire.",
    MigrationOptions = new MongoMigrationOptions
    {
        MigrationStrategy = new MigrateMongoMigrationStrategy(),
        BackupStrategy = new CollectionMongoBackupStrategy()
    }
};
builder.Services.AddHangfire((provider, config) =>
{
    var mongoClient = provider.GetRequiredService<IMongoClient>();
    var mongoSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
    if (mongoSettings == null)
        throw new InvalidOperationException("MongoDB settings are not configured properly");
    config.UseMongoStorage(
        mongoClient,
        mongoSettings.DatabaseName,
        hangfireOptions);
});
builder.Services.AddHangfireServer();


//Serilog with Mongo Sink
builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
{
    var repo = services.GetRequiredService<RecoTrackApi.Repositories.Interfaces.ILogRepository>();
    loggerConfiguration
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.Async(a => a.Sink(new RecoTrackApi.Logging.MongoSerilogSink(repo)));
});

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
            "https://recotrackpiyushsingh.vercel.app",
            "http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

//JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "RecoTrackAPI",
            ValidAudience = "RecoTrackWeb",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

//Controllers & SignalR
builder.Services.AddControllers();
builder.Services.AddSignalR();

var app = builder.Build();

//correlationIdMiddleware
app.UseMiddleware<CorrelationIdMiddleware>();

//Header Validation Middleware
app.UseMiddleware<HeaderValidationMiddleware>();

//global Exception Handler Middleware 
app.UseMiddleware<GlobalExceptionMiddleware>();

//Hangfire Dashboard
var dashboardOptions = new DashboardOptions
{
    Authorization = new[] { new HangfireDashboardAuthorizationFilter() }
};
app.UseHangfireDashboard("/hangfire", dashboardOptions);

// Register recurring job: every 5 minutes
RecurringJob.AddOrUpdate<LogCleanupJob>(
    "log-cleanup",
    job => job.ExecuteAsync(),
    "*/5 * * * *"
);

//Logging Startup Info
Log.Information("Application Environment: {Environment}", app.Environment.EnvironmentName);

//Middleware
app.UseSerilogRequestLogging();
app.UseRequestTiming();
app.UseCors("FrontendPolicy");
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHub<RecoTrackApi.Hubs.NotificationHub>("/notificationHub");
app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok("RecoTrack API is running, Credits - PIYUSH SINGH!"));

app.Run();

