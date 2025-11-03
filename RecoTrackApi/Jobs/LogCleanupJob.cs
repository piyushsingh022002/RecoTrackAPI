using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Services;

namespace RecoTrackApi.Jobs
{
    public class LogCleanupJob
    {
        private readonly ILogCleanerService _cleaner;
        private readonly ILogger<LogCleanupJob> _logger;

        public LogCleanupJob(ILogCleanerService cleaner, ILogger<LogCleanupJob> logger)
        {
            _cleaner = cleaner;
            _logger = logger;
        }

        public async Task ExecuteAsync()
        {
            try
            {
                var deleted = await _cleaner.CleanAllAsync();
                _logger.LogInformation("LogCleanupJob deleted {Deleted} documents", deleted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LogCleanupJob failed");
                throw; // ensure Hangfire records failure
            }
        }
    }
}
