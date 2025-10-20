using Serilog.Core;
using Serilog.Events;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Models;


namespace RecoTrackApi.Logging
{
    public class MongoSerilogSink : ILogEventSink, IDisposable
    {
        private readonly ILogRepository _repo;
        private readonly IFormatProvider _formatProvider;

        public MongoSerilogSink(ILogRepository repo, IFormatProvider formatProvider = null)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _formatProvider = formatProvider;
        }

        public void Emit(LogEvent logEvent)
        {
            try
            {
                var renderedMessage = logEvent.RenderMessage(_formatProvider);

                var doc = new LogEntry
                {
                    Timestamp = logEvent.Timestamp.UtcDateTime,
                    Level = logEvent.Level.ToString(),
                    Message = renderedMessage,
                    Exception = logEvent.Exception?.ToString(),
                    SourceContext = logEvent.Properties.TryGetValue("SourceContext", out var sc) ? sc.ToString().Trim('"') : null,
                    Properties = logEvent.Properties.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.ToString() ?? string.Empty)
                };

                // Insert synchronously. We will configure Serilog to call this sink asynchronously
                _repo.Insert(doc);
            }
            catch
            {
                // Swallow to ensure logging doesn't throw. You can write to Console/File if wanted.
            }
        }

        public void Dispose()
        {
            // nothing to dispose here; if your repo requires disposal, handle it.
        }
    }

}
