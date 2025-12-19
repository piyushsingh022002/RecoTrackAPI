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
using RecoTrackApi.Logging;
using RecoTrackApi.Repositories;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Services;
using RecoTrackApi.Services.Interfaces;
using Serilog;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

//Serilog Bootstrap Logger
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
//Serilog with Mongo Sink
builder.Host.UseSerilog(SerilogConfiguration.ConfigureSerilog);

//Configuration
var configuration = builder.Configuration;

//mongo configurations
builder.Services.AddMongo(builder.Configuration);

var jwtKey = configuration["JwtSettings:SecretKey"];
if (string.IsNullOrEmpty(jwtKey))
    throw new InvalidOperationException("JWT secret key is not configured");


//Health Checks
builder.Services.AddHealthChecks();

//applicaton layer service extension
builder.Services.AddApplication();

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

//API Services
builder.Services.AddApi(builder.Configuration);

//infrastructure layer service extension
builder.Services.AddInfrastructure(builder.Configuration);


var app = builder.Build();

//Logging Startup Info
Log.Information("Application Environment: {Environment}", app.Environment.EnvironmentName);

app.UseApiMiddlewares();
app.UseSecurity();
app.UseObservability();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapApiEndpoints();

app.Run();

