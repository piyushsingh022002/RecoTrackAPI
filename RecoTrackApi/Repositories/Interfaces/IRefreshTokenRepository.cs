using RecoTrackApi.Models;

namespace RecoTrackApi.Repositories.Interfaces
{
 public interface IRefreshTokenRepository
 {
 Task SaveAsync(RefreshTokenEntry entry);
 Task<RefreshTokenEntry?> GetByTokenHashAsync(string tokenHash);
 Task RevokeAsync(string tokenHash, string? replacedByTokenHash = null);
 Task DeleteExpiredAsync();
 }
}