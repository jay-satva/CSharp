using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MyProject.Domain.Entities
{
    public class User
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

        [BsonElement("firstName")]
        public string FirstName { get; set; } = string.Empty;

        [BsonElement("lastName")]
        public string LastName { get; set; } = string.Empty;

        [BsonElement("name")]
        public string? LegacyName { get; set; }

        [BsonElement("email")]
        public string Email { get; set; } = string.Empty;

        [BsonElement("phoneNumber")]
        public string? PhoneNumber { get; set; }

        [BsonElement("passwordHash")]
        public string? PasswordHash { get; set; }

        [BsonElement("authProvider")]
        public string AuthProvider { get; set; } = "manual";

        [BsonElement("intuitSubId")]
        public string? IntuitSubId { get; set; }

        [BsonElement("profilePhotoUrl")]
        public string? ProfilePhotoUrl { get; set; }

        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("updatedAt")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [BsonElement("refreshTokens")]
        public List<RefreshToken> RefreshTokens { get; set; } = new();

        [BsonIgnore]
        public string Name
        {
            get
            {
                var fullName = $"{FirstName} {LastName}".Trim();
                if (!string.IsNullOrWhiteSpace(fullName))
                    return fullName;
                return LegacyName ?? string.Empty;
            }
        }
    }
}
