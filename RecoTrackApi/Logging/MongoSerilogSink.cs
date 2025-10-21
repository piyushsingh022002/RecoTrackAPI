using Serilog.Core;
using Serilog.Events;
using RecoTrackApi.Repositories.Interfaces;
using RecoTrackApi.Models;

namespace RecoTrackApi.Logging
{
    public class MongoSerilogSink : ILogEventSink, IDisposable
    {
        private readonly ILogRepository _repo;
        private readonly IFormatProvider? _formatProvider;

        public MongoSerilogSink(ILogRepository repo, IFormatProvider? formatProvider = null)
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
                    Exception = logEvent.Exception?.ToString() ?? string.Empty,
                    SourceContext = logEvent.Properties.TryGetValue("SourceContext", out var sc) ? sc.ToString().Trim('"') : string.Empty,
                    Properties = logEvent.Properties?.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value?.ToString() ?? string.Empty
                    ) ?? new Dictionary<string, string>()
                };

                _repo.Insert(doc);
            }
            catch
            (Exception ex)
            {
                try
                {
                    Console.Error.WriteLine($"[MongoSerilogSink] Failed to write log: {ex.Message}");
                    Console.Error.WriteLine(ex.ToString());
                }
                catch
                {
                    // Swallow all to prevent crash in Emit
                }
            }
        }

        public void Dispose()
        {
            if (_repo is IDisposable disposableRepo)
            {
                disposableRepo.Dispose();
            }
        }
    }
}
