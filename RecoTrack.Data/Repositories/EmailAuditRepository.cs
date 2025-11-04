using Microsoft.Extensions.Options;
using MongoDB.Driver;
using RecoTrack.Application.Interfaces;
using RecoTrack.Application.Models;
using RecoTrack.Shared.Settings;

namespace RecoTrack.Data.Repositories
{

    public class EmailAuditRepository : IEmailAuditRepository
    {
        private readonly IMongoCollection<EmailAuditRecord> _col;

        public EmailAuditRepository(IMongoDatabase database, IOptions<MongoDbSettings> settings)
        {
            var collectionName = settings.Value.EmailAuditCollectionName ?? "email_audit";
            _col = database.GetCollection<EmailAuditRecord>(collectionName);
        }

        public async Task AddAsync(EmailAuditRecord record)
        {
            await _col.InsertOneAsync(record);
        }
    }

}
