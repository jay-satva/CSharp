namespace FinalExam.Data
{
    using MongoDB.Driver;
    using FinalExam.Models;
    using MongoDB.Bson;

    public class CompanyRepository
    {
        private readonly MongoDbContext _context;

        public CompanyRepository(MongoDbContext context)
        {
            _context = context;
            EnsureIndexes();
        }

        public async Task<List<Company>> GetByUserIdAsync(string userId) =>
            await _context.Companies.Find(c => c.UserId == userId).ToListAsync();

        public async Task<List<Company>> GetActiveByUserIdAsync(string userId) =>
            await _context.Companies.Find(c => c.UserId == userId && c.IsActive).ToListAsync();

        public async Task<Company?> GetByUserIdAndRealmIdAsync(string userId, string realmId) =>
            await _context.Companies.Find(c => c.UserId == userId && c.RealmId == realmId).FirstOrDefaultAsync();

        public async Task<Company?> GetActiveByUserIdAndRealmIdAsync(string userId, string realmId) =>
            await _context.Companies.Find(c => c.UserId == userId && c.RealmId == realmId && c.IsActive).FirstOrDefaultAsync();

        public async Task UpsertConnectedCompanyAsync(string userId, Company company)
        {
            company.UserId = userId;

            var existing = await GetByUserIdAndRealmIdAsync(userId, company.RealmId);
            if (existing == null)
            {
                await _context.Companies.InsertOneAsync(company);
                return;
            }

            company.Id = existing.Id;
            await _context.Companies.ReplaceOneAsync(
                c => c.UserId == userId && c.RealmId == company.RealmId,
                company);
        }

        public async Task<bool> SetCompanyActiveAsync(string userId, string realmId, bool isActive)
        {
            var existing = await GetByUserIdAndRealmIdAsync(userId, realmId);
            if (existing == null) return false;

            existing.IsActive = isActive;
            existing.LinkedAt = isActive ? DateTime.UtcNow : existing.LinkedAt;

            await _context.Companies.ReplaceOneAsync(
                c => c.UserId == userId && c.RealmId == realmId,
                existing);

            return true;
        }

        private void EnsureIndexes()
        {
            var existingIndexes = _context.Companies.Indexes.List().ToList();
            var existingIndexNames = existingIndexes
                .Select(index => index.GetValue("name", BsonNull.Value))
                .Where(name => !name.IsBsonNull && name.IsString)
                .Select(name => name.AsString)
                .ToHashSet(StringComparer.Ordinal);

            if (!existingIndexNames.Contains("ux_companies_user_realm"))
            {
                var index = new CreateIndexModel<Company>(
                    Builders<Company>.IndexKeys
                        .Ascending(c => c.UserId)
                        .Ascending(c => c.RealmId),
                    new CreateIndexOptions
                    {
                        Unique = true,
                        Name = "ux_companies_user_realm"
                    });

                _context.Companies.Indexes.CreateOne(index);
            }
        }
    }
}
