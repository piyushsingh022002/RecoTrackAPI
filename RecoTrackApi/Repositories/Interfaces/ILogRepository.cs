using RecoTrackApi.Models;

namespace RecoTrackApi.Repositories.Interfaces
{
    public interface ILogRepository
    {
        void Insert(LogEntry entry);
        Task InsertAsync(LogEntry entry);
        Task ClearLogsAsync();

    }
}
