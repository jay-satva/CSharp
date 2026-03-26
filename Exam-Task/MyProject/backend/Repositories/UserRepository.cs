using MongoDB.Driver;
using MyProject.Domain.Entities;
using MyProject.Infrastructure.Data;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MongoDbContext _context;

        public UserRepository(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(string id)
        {
            return await _context.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.Find(u => u.Email == email.ToLower()).FirstOrDefaultAsync();
        }

        public async Task<User?> GetByIntuitSubIdAsync(string intuitSubId)
        {
            var filter = Builders<User>.Filter.Or(
                Builders<User>.Filter.Eq("intuitSubId", intuitSubId),
                Builders<User>.Filter.Eq("intuitUserId", intuitSubId));

            return await _context.Users.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<User?> FindByRefreshTokenAsync(string token)
        {
            return await _context.Users.Find(u => u.RefreshTokens.Any(t => t.Token == token)).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(User user)
        {
            await _context.Users.InsertOneAsync(user);
        }

        public async Task UpdateAsync(User user)
        {
            await _context.Users.ReplaceOneAsync(u => u.Id == user.Id, user);
        }
    }
}
