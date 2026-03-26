using MongoDB.Driver;
using MyProject.Domain.Entities;
using MyProject.Infrastructure.Data;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Infrastructure.Repositories
{
    public class CompanyRepository : ICompanyRepository
    {
        private readonly MongoDbContext _context;

        public CompanyRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<Company?> GetByUserIdAsync(string userId)
        {
            return await _context.Companies.Find(c => c.UserId == userId && c.IsConnected)
                .SortByDescending(c => c.ConnectedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<Company?> GetByRealmIdAsync(string realmId)
        {
            return await _context.Companies.Find(c => c.RealmId == realmId).FirstOrDefaultAsync();
        }

        public async Task<Company?> GetByIdAsync(string id)
        {
            return await _context.Companies.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Company?> GetByUserAndRealmIdAsync(string userId, string realmId)
        {
            return await _context.Companies.Find(c => c.UserId == userId && c.RealmId == realmId).FirstOrDefaultAsync();
        }

        public async Task<List<Company>> GetAllByUserIdAsync(string userId)
        {
            return await _context.Companies.Find(c => c.UserId == userId)
                .SortByDescending(c => c.ConnectedAt)
                .ToListAsync();
        }

        public async Task<List<Company>> GetConnectedByUserIdAsync(string userId)
        {
            return await _context.Companies.Find(c => c.UserId == userId && c.IsConnected)
                .SortByDescending(c => c.ConnectedAt)
                .ToListAsync();
        }

        public async Task CreateAsync(Company company)
        {
            await _context.Companies.InsertOneAsync(company);
        }

        public async Task UpdateAsync(Company company)
        {
            await _context.Companies.ReplaceOneAsync(c => c.Id == company.Id, company);
        }

        public async Task DeleteAsync(string id)
        {
            await _context.Companies.DeleteOneAsync(c => c.Id == id);
        }
    }
}
