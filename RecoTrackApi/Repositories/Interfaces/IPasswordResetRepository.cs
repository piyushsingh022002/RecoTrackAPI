using RecoTrackApi.Models;

namespace RecoTrackApi.Repositories.Interfaces
{
    public interface IPasswordResetRepository
    {
        Task SaveAsync(PasswordResetEntry entry);
        Task DeactivateActiveOtpsAsync(string email);
        Task<PasswordResetEntry?> GetActiveUnexpiredEntryAsync(string email, string otp);
        Task SetSuccessCodeAsync(string email, string otp, string successCode);
        Task<PasswordResetEntry?> GetBySuccessCodeAsync(string email, string successCode);
    }
}
