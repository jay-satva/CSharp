namespace FinalExam.Data
{
    using MongoDB.Driver;
    using FinalExam.Models;

    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;
        private readonly string _companiesCollectionName;

        public MongoDbContext(IConfiguration configuration)
        {
            var client = new MongoClient(configuration.GetValue<string>("MongoDbSettings:ConnectionString"));
            _database = client.GetDatabase(configuration.GetValue<string>("MongoDbSettings:DatabaseName"));

            _companiesCollectionName =
                configuration.GetValue<string>("MongoDbSettings:Collections:CompanyCollection") ?? "Companies";
        }

        public IMongoCollection<Company> Companies => 
            _database.GetCollection<Company>(_companiesCollectionName);
    }
}
