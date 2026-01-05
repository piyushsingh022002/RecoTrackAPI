using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Interfaces
{
    public interface IInternalHttpClient
    {
        Task<TResponse> PostAsync<TRequest, TResponse>(
            string url,
            TRequest body,
            string userJwt,
            CancellationToken cancellationToken = default);

        Task<TResponse> GetAsync<TResponse>(
            string url,
            string userJwt,
            CancellationToken cancellationToken = default);
    }
}
