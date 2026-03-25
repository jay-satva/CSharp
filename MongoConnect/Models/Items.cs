using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoConnect.Models
{
    public class Items
    {
        [BsonId]
        [BsonRepresentation(BsonType.String)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Price { get; set; }
    }
}
