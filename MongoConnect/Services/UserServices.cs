using Microsoft.AspNetCore.Identity.Data;
using MongoConnect.Models;
using MongoDB.Driver;

namespace MongoConnect.Services
{
    public class UserServices
    {
        private readonly IMongoCollection<Users> _users;
        public UserServices(IMongoClient client, MongoDbSettings settings)
        {
            var database = client.GetDatabase(settings.DatabaseName);
            _users = database.GetCollection<Users>(settings.Collections.UsersCollection);
        }
        public async Task<Users?> GetByEmailAsync(string email) => await _users.Find(u => u.Email == email).FirstOrDefaultAsync();
        public async Task<List<Users>> GetAllAsync() => await _users.Find(_ => true).ToListAsync();
        public async Task<Users?> GetByIdAsync(Guid id) => await _users.Find(u => u.Id == id).FirstOrDefaultAsync();
        
        public async Task<Users> CreateAsync(RegisterReq request)
        {
            var user = new Users
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };
            await _users.InsertOneAsync(user);
            return user;
        }
        public async Task<bool> UpdateAsync(Guid id, Users updated)
        {
            if (!updated.Password.StartsWith("$2")) updated.Password = BCrypt.Net.BCrypt.HashPassword(updated.Password);

            var result = await _users.ReplaceOneAsync(u => u.Id == id, updated);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _users.DeleteOneAsync(u => u.Id == id);
            return result.DeletedCount > 0;
        }

        public bool VerifyPassword(string plainText, string hash) => BCrypt.Net.BCrypt.Verify(plainText, hash);
    }
}