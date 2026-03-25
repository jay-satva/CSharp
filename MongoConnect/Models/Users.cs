using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MongoConnect.Models
{
    public class Users
    {
        [BsonId]
        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
