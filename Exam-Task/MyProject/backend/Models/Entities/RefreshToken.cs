using MongoDB.Bson.Serialization.Attributes;

namespace MyProject.Domain.Entities
{
    public class RefreshToken
    {
        [BsonElement("token")]
        public string Token { get; set; } = string.Empty;

        [BsonElement("expires")]
        public DateTime Expires { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("isRevoked")]
        public bool IsRevoked { get; set; } = false;

        [BsonElement("replacedByToken")]
        public string? ReplacedByToken { get; set; }

        public bool IsExpired => DateTime.UtcNow >= Expires;
        public bool IsActive => !IsRevoked && !IsExpired;
    }
}