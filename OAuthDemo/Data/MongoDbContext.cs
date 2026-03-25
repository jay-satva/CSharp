namespace OAuthDemo.Data;
using MongoDB.Driver;
using OAuthDemo.Models;
using OAuthDemo.Settings;
public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IConfiguration config)
        {
            var settings = config.GetSection("MongoDbSettings").Get<MongoDbSettings>();
            var client = new MongoClient(settings.ConnectionString);
            _database = client.GetDatabase(settings.DatabaseName);
        }

        public IMongoCollection<QuickBooksToken> Tokens => _database.GetCollection<QuickBooksToken>("QuickBooksTokens");
}

