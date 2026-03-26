using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinalExam.Models;

[BsonIgnoreExtraElements]
public class AppUser
{
    [BsonId]
    [BsonIgnoreIfNull]
    public BsonValue? Id { get; set; }

    public string UserId { get; set; } = Guid.NewGuid().ToString();
    public string? IntuitSub { get; set; }

    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? PasswordHash { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

