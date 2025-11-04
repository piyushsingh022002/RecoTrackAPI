using RecoTrack.Application.Models;

namespace RecoTrack.Application.Interfaces
{
    public interface IEmailAuditRepository
    {
        Task AddAsync(EmailAuditRecord record);
    }
}