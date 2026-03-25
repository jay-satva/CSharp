using MongoDB.Driver;
using MongoConnect.Models;
namespace MongoConnect.Services
{
    public class ItemServices
    {
        private readonly IMongoCollection<Items> _items;
        public ItemServices(IMongoClient client, MongoDbSettings settings)
        {
            var database = client.GetDatabase(settings.DatabaseName);
            _items = database.GetCollection<Items>(settings.Collections.ItemsCollection);
        }
        public async Task<List<Items>> GetAllAsync() => await _items.Find(_ => true).ToListAsync();
        public async Task<Items?> GetByIdAsync(Guid id) => await _items.Find(i => i.Id == id).FirstOrDefaultAsync();
        public async Task<Items> CreateAsync(Items item)
        {
            item.Id = Guid.NewGuid();
            await _items.InsertOneAsync(item);
            return item;
        }

        public async Task<bool> UpdateAsync(Guid id, Items updated)
        {
            updated.Id = id;
            var result = await _items.ReplaceOneAsync(i => i.Id == id, updated);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var result = await _items.DeleteOneAsync(i => i.Id == id);
            return result.DeletedCount > 0;
        }
    }
}