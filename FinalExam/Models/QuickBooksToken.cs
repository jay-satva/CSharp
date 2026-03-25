using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FinalExam.Models;

[BsonIgnoreExtraElements]
public class Company
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }
    public string UserId { get; set; } = null!;
    public string RealmId { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public string Country { get; set; } = null!;
    public bool IsActive { get; set; } = true;
    public DateTime LinkedAt { get; set; } = DateTime.UtcNow;
}
