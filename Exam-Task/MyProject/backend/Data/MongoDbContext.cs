using MongoDB.Driver;
using MyProject.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace MyProject.Infrastructure.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration configuration)
        {
            var connectionString = configuration["MongoDB:ConnectionString"];
            var databaseName = configuration["MongoDB:DatabaseName"];
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
            EnsureIndexes();
        }

        public IMongoCollection<User> Users => _database.GetCollection<User>("users");
        public IMongoCollection<Company> Companies => _database.GetCollection<Company>("companies");

        private void EnsureIndexes()
        {
            var users = Users;
            var emailIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.Email),
                new CreateIndexOptions { Unique = true, Name = "ux_users_email" });

            var intuitSubIndex = new CreateIndexModel<User>(
                Builders<User>.IndexKeys.Ascending(u => u.IntuitSubId),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_users_intuitSubId",
                    Sparse = true
                });

            users.Indexes.CreateMany(new[] { emailIndex, intuitSubIndex });

            var companies = Companies;
            var userRealmIndex = new CreateIndexModel<Company>(
                Builders<Company>.IndexKeys
                    .Ascending(c => c.UserId)
                    .Ascending(c => c.RealmId),
                new CreateIndexOptions
                {
                    Unique = true,
                    Name = "ux_companies_user_realm"
                });

            companies.Indexes.CreateOne(userRealmIndex);
        }
    }
}
