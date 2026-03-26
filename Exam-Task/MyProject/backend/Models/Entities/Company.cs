using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyProject.Domain.Entities
{
    public class Company
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("userId")]
        public string UserId { get; set; } = string.Empty;

        [BsonElement("realmId")]
        public string RealmId { get; set; } = string.Empty;

        [BsonElement("companyName")]
        public string CompanyName { get; set; } = string.Empty;

        [BsonElement("accessToken")]
        public string AccessToken { get; set; } = string.Empty;

        [BsonElement("refreshToken")]
        public string RefreshToken { get; set; } = string.Empty;

        [BsonElement("accessTokenExpiry")]
        public DateTime AccessTokenExpiry { get; set; }

        [BsonElement("refreshTokenExpiry")]
        public DateTime RefreshTokenExpiry { get; set; }

        [BsonElement("connectedAt")]
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isConnected")]
        public bool IsConnected { get; set; } = true;
    }
}