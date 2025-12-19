using RecoTrackApi.Repositories.Interfaces;
using Serilog;

namespace RecoTrackApi.Logging
{
    public static class SerilogConfiguration
    {
        public static void ConfigureSerilog(
            HostBuilderContext context,
            IServiceProvider services,
            LoggerConfiguration loggerConfiguration)
        {
            var logRepository = services.GetRequiredService<ILogRepository>();

            loggerConfiguration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Async(a =>
                    a.Sink(new Logging.MongoSerilogSink(logRepository)));
        }
    }
}
