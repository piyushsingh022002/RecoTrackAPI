using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Shared.Contracts.Errors
{
    public sealed class ApiErrorResponse
    {
        public int Status { get; init; }
        public string ErrorCode { get; init; } = default!;
        public string Message { get; init; } = default!;
        public string CorrelationId { get; init; } = default!;
        public string? ClientId { get; init; }
        public string Path { get; init; } = default!;
        public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    }
}
