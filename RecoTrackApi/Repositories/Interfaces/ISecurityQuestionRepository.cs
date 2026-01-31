using System.Threading.Tasks;
using RecoTrackApi.Models;

namespace RecoTrackApi.Repositories.Interfaces
{
    public interface ISecurityQuestionRepository
    {
        Task SaveAsync(SecurityQuestionEntry entry);
        Task<SecurityQuestionEntry?> GetByUserIdAsync(string userId);
    }
}
