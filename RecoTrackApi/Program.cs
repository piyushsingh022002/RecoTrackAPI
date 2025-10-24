using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.SignalR;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using RecoTrack.Application.Interfaces;
using Serilog;
using RecoTrackApi.Configurations;
using RecoTrackApi.Extensions;
using RecoTrackApi.Repositories;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;
using RecoTrackApi.Services.Interfaces;
using System.Text;
using RecoTrack.Infrastructure.Services;


var builder = WebApplication.CreateBuilder(args);

// ----- Serilog Bootstrap Logger -----
// (Logs to console until DI container is built)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// ----- MongoDB -----
var mongoSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
if (mongoSettings == null)
{
    throw new InvalidOperationException("MongoDB settings are not configured properly");
}

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(mongoSettings.DatabaseName);
    return database;
});

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = mongoSettings.ConnectionString;
    Log.Information("Connecting to MongoDB at: {ConnectionString}",
        connectionString.StartsWith("mongodb://localhost") ? "localhost" : "production");
    return new MongoClient(connectionString);
});

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(nameof(MongoDbSettings)));

// ----- Dependency Injection -----

builder.Services.AddSingleton<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();

builder.Services.AddSingleton<ILogRepository, LogRepository>();

builder.Services.AddScoped<IAutomatedPrReviewService, AutomatedPrReviewService>();
builder.Services.AddHttpClient<IAutomatedPrReviewService, AutomatedPrReviewService>();
builder.Services.AddHttpClient<IGitHubClientService, GitHubClientService>();



// ----- Serilog with Mongo Sink -----
builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
{
    var repo = services.GetRequiredService<ILogRepository>();

    loggerConfiguration
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.Async(a => a.Sink(new RecoTrackApi.Logging.MongoSerilogSink(repo)));
});

// ----- CORS ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
    "https://recotrackpiyushsingh.vercel.app", // ✅ Your actual frontend domain
    "http://localhost:5173"
)
.AllowAnyHeader()
.AllowAnyMethod()
.AllowCredentials(); // ✅ THIS IS MANDATORY for SignalR + JWT

    });
});

// ----- JWT -----
var jwtKey = builder.Configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtKey))
{
    throw new InvalidOperationException("JWT secret key is not configured");
}

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

// ----- Controllers -----
builder.Services.AddControllers();
builder.Services.AddScoped<INoteRepository, NoteRepository>();
builder.Services.AddScoped<INoteService, NoteService>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddSingleton<IUserIdProvider, SignalRUserIdProvider>();


builder.Services.AddSignalR();

// ----- Swagger -----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "StudentRoutineTracker API", Version = "v1" });
    var securitySchema = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter JWT Bearer token like this: Bearer {token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", securitySchema);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securitySchema, new[] { "Bearer" } }
    });
});

var app = builder.Build();

// Log environment and MongoDB connection info at startup
Log.Information("Application Environment: {Environment}", 
    app.Environment.EnvironmentName);
Log.Information("MongoDB Database: {Database}", mongoSettings.DatabaseName);

app.UseSerilogRequestLogging();
app.UseRequestTiming();

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
};

app.MapControllers();
app.MapHub<RecoTrackApi.Hubs.NotificationHub>("/notificationHub");
app.MapGet("/", () => Results.Ok("RecoTrack API is running, Credits - PIYUSH SINGH!"));


var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Urls.Add($"http://0.0.0.0:{port}");
app.Run();

