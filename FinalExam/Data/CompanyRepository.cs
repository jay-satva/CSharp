namespace FinalExam.Data
{
    using MongoDB.Driver;
    using FinalExam.Models;

    public class CompanyRepository
    {
        private readonly MongoDbContext _context;

        public CompanyRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<List<Company>> GetByUserIdAsync(string userId) =>
            await _context.Companies.Find(c => c.UserId == userId).ToListAsync();

        public async Task<Company?> GetByUserIdAndRealmIdAsync(string userId, string realmId) =>
            await _context.Companies.Find(c => c.UserId == userId && c.RealmId == realmId).FirstOrDefaultAsync();

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
    }
}
