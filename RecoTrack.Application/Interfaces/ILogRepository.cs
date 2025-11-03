using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Interfaces
{
    public interface ILogRepository
    {
        /// <summary>
        /// Delete all logs (or matching filter) and return deleted count
        /// </summary>
        Task<long> DeleteAllAsync();


        /// <summary>
        /// Optionally delete logs older than the provided time and return deleted count
        /// </summary>
        Task<long> DeleteOlderThanAsync(DateTime cutoffUtc);
    }
}
