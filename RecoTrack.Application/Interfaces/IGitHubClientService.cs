using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecoTrack.Application.Interfaces
{
    public interface IGitHubClientService
    {
        /// <summary>
        /// Post a single comment on a GitHub Pull Request (issues API).
        /// repo format: "owner/repo"
        /// </summary>
        Task PostPrCommentAsync(string repo, int prNumber, string commentBody);
    }
}
