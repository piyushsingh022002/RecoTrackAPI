using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Interfaces
{
    public interface IJobMetricsRepository
    {
        Task AddMetricAsync(string jobName, DateTime runAtUtc, long deletedCount);
        Task<JobMetric?> GetLatestAsync(string jobName);
    }


    public record JobMetric(string JobName, DateTime RunAtUtc, long DeletedCount);
}
