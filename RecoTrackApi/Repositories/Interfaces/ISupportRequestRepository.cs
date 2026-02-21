using RecoTrackApi.Models;

namespace RecoTrackApi.Repositories.Interfaces
{
 public interface ISupportRequestRepository
 {
 Task SaveAsync(SupportRequestEntry entry);
 }
}
