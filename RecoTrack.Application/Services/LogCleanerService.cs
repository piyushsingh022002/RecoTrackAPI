using RecoTrack.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Services
{
    public interface ILogCleanerService
    {
        Task<long> CleanAllAsync();
        Task<long> CleanOlderThanAsync(DateTime cutoffUtc);
    }


    public class LogCleanerService : ILogCleanerService
    {
        private readonly ILogRepository _logRepository;
        private readonly IJobMetricsRepository _metricsRepository;


        public LogCleanerService(ILogRepository logRepository, IJobMetricsRepository metricsRepository)
        {
            _logRepository = logRepository;
            _metricsRepository = metricsRepository;
        }


        public async Task<long> CleanAllAsync()
        {
            var deleted = await _logRepository.DeleteAllAsync();
            await _metricsRepository.AddMetricAsync("log-cleanup", DateTime.UtcNow, deleted);
            return deleted;
        }


        public async Task<long> CleanOlderThanAsync(DateTime cutoffUtc)
        {
            var deleted = await _logRepository.DeleteOlderThanAsync(cutoffUtc);
            await _metricsRepository.AddMetricAsync("log-cleanup", DateTime.UtcNow, deleted);
            return deleted;
        }
    }
}
