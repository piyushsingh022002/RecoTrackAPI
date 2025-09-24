using StudentRoutineTrackerApi.Repositories.Interfaces;
using StudentRoutineTrackerApi.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using StudentRoutineTrackerApi.Configurations;
using StudentRoutineTrackerApi.Repositories;
using StudentRoutineTrackerApi.Services;
using StudentRoutineTrackerApi.Extensions;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using System.Text;
using Serilog;


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

// ----- Serilog with Mongo Sink -----
builder.Host.UseSerilog((hostingContext, services, loggerConfiguration) =>
{
    var repo = services.GetRequiredService<ILogRepository>();

    loggerConfiguration
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
        .WriteTo.Async(a => a.Sink(new StudentRoutineTrackerApi.Logging.MongoSerilogSink(repo)));
});

// ----- CORS -----
var frontendURL = builder.Configuration["FrontendURL"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendPolicy", policy =>
    {
        policy.WithOrigins(
            frontendURL ?? string.Empty, // from config, if set
            "http://localhost:5173",    // local dev
            "https://studentroutinetrackerapi.onrender.com", // deployed backend (for testing, if needed)
            "https://your-frontend-domain.com" // <-- add your deployed frontend domain here
        )
        .AllowAnyHeader()
        .AllowAnyMethod();
        // .AllowCredentials(); // Uncomment if you use cookies/auth
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

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
