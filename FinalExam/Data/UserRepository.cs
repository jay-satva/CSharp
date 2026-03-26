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

    public async Task<AppUser?> GetByIntuitSubAsync(string intuitSub) =>
        await _users.Find(u => u.IntuitSub == intuitSub).FirstOrDefaultAsync();

    public async Task<AppUser?> GetByUserIdAsync(string userId) =>
        await _users.Find(u => u.UserId == userId).FirstOrDefaultAsync();

    public async Task CreateAsync(AppUser user)
    {
        user.Email = NormalizeEmail(user.Email);
        user.UserId = EnsureUserId(user.UserId);
        await _users.InsertOneAsync(user);
    }

    public async Task UpdateAsync(AppUser user)
    {
        user.UserId = EnsureUserId(user.UserId);
        await _users.ReplaceOneAsync(u => u.Id == user.Id, user);
    }

    public async Task<AppUser> UpsertIntuitUserAsync(
        string? intuitSub,
        string? email,
        string? targetUserId,
        string? name,
        bool linkIntuitSub = true)
    {
        var normalizedEmail = NormalizeEmail(email);
        AppUser? existing = null;

        if (!string.IsNullOrWhiteSpace(targetUserId))
            existing = await GetByUserIdAsync(targetUserId);

        if (existing == null && !string.IsNullOrWhiteSpace(intuitSub))
            existing = await GetByIntuitSubAsync(intuitSub);

        if (existing == null && !string.IsNullOrWhiteSpace(normalizedEmail))
            existing = await GetByEmailAsync(normalizedEmail);

        if (existing == null)
        {
            existing = new AppUser
            {
                UserId = Guid.NewGuid().ToString(),
                IntuitSub = string.IsNullOrWhiteSpace(intuitSub) ? null : intuitSub,
                Email = normalizedEmail,
                Name = string.IsNullOrWhiteSpace(name)
                    ? (!string.IsNullOrWhiteSpace(normalizedEmail) ? normalizedEmail : "Intuit User")
                    : name.Trim(),
                PasswordHash = null,
                CreatedAt = DateTime.UtcNow
            };
        }

        existing.UserId = EnsureUserId(existing.UserId);
        if (linkIntuitSub && !string.IsNullOrWhiteSpace(intuitSub))
        {
            var owner = await GetByIntuitSubAsync(intuitSub);
            if (owner == null || owner.Id == existing.Id)
                existing.IntuitSub = intuitSub;
        }
        if (string.IsNullOrWhiteSpace(existing.Email) && !string.IsNullOrWhiteSpace(normalizedEmail))
            existing.Email = normalizedEmail;

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
        var existingIndexNames = existingIndexes
            .Select(index => index.GetValue("name", BsonNull.Value))
            .Where(name => !name.IsBsonNull && name.IsString)
            .Select(name => name.AsString)
            .ToHashSet(StringComparer.Ordinal);

        DropIndexIfExists(existingIndexNames, "UserId_1");
        DropIndexIfExists(existingIndexNames, "Email_1");
        DropIndexIfExists(existingIndexNames, "IntuitSub_1");

        var emailIndexModel = new CreateIndexModel<AppUser>(
            Builders<AppUser>.IndexKeys.Ascending(u => u.Email),
            new CreateIndexOptions<AppUser>
            {
                Unique = true,
                Name = "Email_1",
                PartialFilterExpression = new BsonDocument
                {
                    {
                        "Email",
                        new BsonDocument
                        {
                            { "$exists", true },
                            { "$type", "string" },
                            { "$gt", "" }
                        }
                    }
                }
            });

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

        var intuitSubIndexModel = new CreateIndexModel<AppUser>(
            Builders<AppUser>.IndexKeys.Ascending(u => u.IntuitSub),
            new CreateIndexOptions<AppUser>
            {
                Unique = true,
                Name = "IntuitSub_1",
                PartialFilterExpression = new BsonDocument
                {
                    {
                        "IntuitSub",
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
        _users.Indexes.CreateOne(intuitSubIndexModel);
    }

    private void DropIndexIfExists(HashSet<string> existingIndexNames, string indexName)
    {
        if (!existingIndexNames.Contains(indexName))
            return;

        try
        {
            _users.Indexes.DropOne(indexName);
        }
        catch (MongoCommandException ex) when (ex.Message.Contains("index not found", StringComparison.OrdinalIgnoreCase))
        {
        }
    }

    private static string EnsureUserId(string? userId) =>
        string.IsNullOrWhiteSpace(userId) ? Guid.NewGuid().ToString() : userId;

    private static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
}
