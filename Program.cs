using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MongoDB.Driver;
using Serilog;
using StudentRoutineTrackerApi.Configurations;
using StudentRoutineTrackerApi.Repositories;
using StudentRoutineTrackerApi.Repositories.Interfaces;
using StudentRoutineTrackerApi.Services;
using StudentRoutineTrackerApi.Services.Interfaces;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ----- Serilog Logging -----
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog();

// ----- MongoDB -----
var mongoSettings = builder.Configuration.GetSection(nameof(MongoDbSettings)).Get<MongoDbSettings>();
if (mongoSettings == null)
{
    throw new InvalidOperationException("MongoDB settings are not configured properly");
}

builder.Services.AddSingleton<IMongoClient>(sp =>
{
    var connectionString = mongoSettings.ConnectionString;
    Log.Information("Connecting to MongoDB at: {ConnectionString}", 
        connectionString.StartsWith("mongodb://localhost") ? "localhost" : "production");
    return new MongoClient(connectionString);
});

builder.Services.AddScoped<IMongoDatabase>(sp =>
{
    var client = sp.GetRequiredService<IMongoClient>();
    var database = client.GetDatabase(mongoSettings.DatabaseName);
    return database;
});

builder.Services.Configure<MongoDbSettings>(
    builder.Configuration.GetSection(nameof(MongoDbSettings)));
builder.Services.AddSingleton<IMongoDbService, MongoDbService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();


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
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
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

app.UseCors("FrontendPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();
