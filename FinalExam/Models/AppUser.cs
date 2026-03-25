using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinalExam.Models;

[BsonIgnoreExtraElements]
public class AppUser
{
    [BsonId]
    public BsonValue? Id { get; set; }

    public string UserId { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PasswordHash { get; set; }

    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public string? RefreshToken { get; set; }
    public string? RealmId { get; set; }
    public DateTime? AccessTokenExpiry { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }
    public DateTime? TokenCreatedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

