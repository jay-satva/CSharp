using FinalExam.Models;
using FinalExam.Settings;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace FinalExam.Data;

public class UserRepository
{
    private readonly IMongoCollection<AppUser> _users;

    public UserRepository(IOptions<MongoDbSettings> settings)
    {
        var client = new MongoClient(settings.Value.ConnectionString);
        var db = client.GetDatabase(settings.Value.DatabaseName);
        _users = db.GetCollection<AppUser>(settings.Value.Collections.UsersCollection);

        BackfillMissingUserIds();
        EnsureIndexes();
    }

    public async Task<AppUser?> GetByEmailAsync(string email) =>
        await _users.Find(u => u.Email == email.Trim().ToLowerInvariant()).FirstOrDefaultAsync();

    public async Task<AppUser?> GetByUserIdAsync(string userId) =>
        await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();

    public async Task CreateAsync(AppUser user)
    {
        user.Email = user.Email.Trim().ToLowerInvariant();
        user.UserId = EnsureUserId(user.UserId);
        await _users.InsertOneAsync(user);
    }

    public async Task UpdateAsync(AppUser user)
    {
        user.UserId = EnsureUserId(user.UserId);
        await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
    }

    public async Task<AppUser> UpsertQuickBooksTokensAsync(
        string email,
        string? name,
        string accessToken,
        string refreshToken,
        string? idToken,
        string? realmId,
        DateTime accessTokenExpiry,
        DateTime refreshTokenExpiry)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await GetByEmailAsync(normalizedEmail);

        if (existing == null)
        {
            existing = new AppUser
            {
                UserId = Guid.NewGuid().ToString(),
                Email = normalizedEmail,
                Name = string.IsNullOrWhiteSpace(name) ? normalizedEmail : name.Trim(),
                PasswordHash = null,
                CreatedAt = DateTime.UtcNow
            };
        }

        existing.UserId = EnsureUserId(existing.UserId);
        existing.AccessToken = accessToken;
        existing.RefreshToken = refreshToken;
        existing.IdToken = idToken;
        existing.RealmId = realmId;
        existing.AccessTokenExpiry = accessTokenExpiry;
        existing.RefreshTokenExpiry = refreshTokenExpiry;
        existing.TokenCreatedAt = existing.TokenCreatedAt ?? DateTime.UtcNow;

        if (string.IsNullOrWhiteSpace(existing.Name) && !string.IsNullOrWhiteSpace(name))
            existing.Name = name.Trim();

        if (existing.Id == null || existing.Id.IsBsonNull)
            await _users.InsertOneAsync(existing);
        else
            await UpdateAsync(existing);

        return existing;
    }

    private void BackfillMissingUserIds()
    {
        var missingUsers = _users.Find(Builders<AppUser>.Filter.Or(
            Builders<AppUser>.Filter.Exists(u => u.UserId, false),
            Builders<AppUser>.Filter.Eq(u => u.UserId, null),
            Builders<AppUser>.Filter.Eq(u => u.UserId, string.Empty)))
            .ToList();

        foreach (var user in missingUsers)
        {
            user.UserId = Guid.NewGuid().ToString();
            _users.ReplaceOne(u => u.Id == user.Id, user);
        }
    }

    private void EnsureIndexes()
    {
        var existingIndexes = _users.Indexes.List().ToList();
        var hasLegacyUserIdIndex = existingIndexes.Any(index =>
            index.TryGetValue("name", out var name) && name == "UserId_1");

        if (hasLegacyUserIdIndex)
            _users.Indexes.DropOne("UserId_1");

        var emailIndexModel = new CreateIndexModel<AppUser>(
            Builders<AppUser>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions { Unique = true, Name = "Email_1" });

        var userIdIndexModel = new CreateIndexModel<AppUser>(
            Builders<AppUser>.IndexKeys.Ascending(u => u.UserId),
            new CreateIndexOptions<AppUser>
            {
                Unique = true,
                Name = "UserId_1",
                PartialFilterExpression = new BsonDocument
                {
                    {
                        "UserId",
                        new BsonDocument
                        {
                            { "$exists", true },
                            { "$type", "string" },
                            { "$gt", "" }
                        }
                    }
                }
            });

        _users.Indexes.CreateOne(emailIndexModel);
        _users.Indexes.CreateOne(userIdIndexModel);
    }

    private static string EnsureUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? Guid.NewGuid().ToString() : userId;
}
