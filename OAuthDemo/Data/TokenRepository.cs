using MongoDB.Driver;
using OAuthDemo.Models;

namespace OAuthDemo.Data
{
    public class TokenRepository
    {
        private readonly MongoDbContext _context;
        public TokenRepository(MongoDbContext context)
        {
            _context = context;
        }
        public async Task SaveTokenAsync(QuickBooksToken token)
        {
            await _context.Tokens.InsertOneAsync(token);
        }
        public async Task<QuickBooksToken> GetByUserIdAsync(string userId)
        {
            return await _context.Tokens.Find(x => x.UserId == userId).FirstOrDefaultAsync();
        }
        public async Task UpdateTokenAsync(string userId, QuickBooksToken updated)
        {
            await _context.Tokens.ReplaceOneAsync(x => x.UserId == userId, updated);
        }
    }
}
