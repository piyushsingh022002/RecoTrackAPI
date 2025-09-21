using StudentRoutineTrackerApi.Models;

namespace StudentRoutineTrackerApi.Repositories.Interfaces
{
    public interface ILogRepository
    {
        void Insert(LogEntry entry);
        Task InsertAsync(LogEntry entry);

    }
}
