using Hangfire.Mongo;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using RecoTrackApi.Extensions;
using RecoTrackApi.Logging;
using Serilog;


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
builder.Services.AddJwtAuthentication(builder.Configuration);

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

