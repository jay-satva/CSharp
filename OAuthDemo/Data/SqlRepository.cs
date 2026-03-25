using Microsoft.Data.SqlClient;
using OAuthDemo.Models;
using System.Data;

namespace OAuthDemo.Data
{
    public class SqlRepository
    {
        private readonly string _connectionString;

        public SqlRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task SaveOrUpdateTokenAsync(QuickBooksToken token)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = @"
                IF EXISTS (SELECT 1 FROM OAuthData WHERE UserId = @UserId)
                BEGIN
                    UPDATE OAuthData SET 
                        AccessToken = @AccessToken,
                        IdToken = @IdToken,
                        RefreshToken = @RefreshToken,
                        RealmId = @RealmId,
                        AccessTokenExpiry = @AccessTokenExpiry,
                        RefreshTokenExpiry = @RefreshTokenExpiry
                    WHERE UserId = @UserId
                END
                ELSE
                BEGIN
                    INSERT INTO OAuthData (UserId, AccessToken, IdToken, RefreshToken, RealmId, AccessTokenExpiry, RefreshTokenExpiry, CreatedAt)
                    VALUES (@UserId, @AccessToken, @IdToken, @RefreshToken, @RealmId, @AccessTokenExpiry, @RefreshTokenExpiry, @CreatedAt)
                END";

            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", token.UserId);
            command.Parameters.AddWithValue("@AccessToken", token.AccessToken);
            command.Parameters.AddWithValue("@IdToken", token.IdToken ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@RefreshToken", token.RefreshToken);
            command.Parameters.AddWithValue("@RealmId", token.RealmId);
            command.Parameters.AddWithValue("@AccessTokenExpiry", token.AccessTokenExpiry);
            command.Parameters.AddWithValue("@RefreshTokenExpiry", token.RefreshTokenExpiry);
            command.Parameters.AddWithValue("@CreatedAt", token.CreatedAt);

            await command.ExecuteNonQueryAsync();
        }

        public async Task<QuickBooksToken> GetByUserIdAsync(string userId)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var query = "SELECT * FROM OAuthData WHERE UserId = @UserId";
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@UserId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new QuickBooksToken
                {
                    UserId = reader["UserId"].ToString(),
                    AccessToken = reader["AccessToken"].ToString(),
                    IdToken = reader["IdToken"] == DBNull.Value ? null : reader["IdToken"].ToString(),
                    RefreshToken = reader["RefreshToken"].ToString(),
                    RealmId = reader["RealmId"].ToString(),
                    AccessTokenExpiry = (DateTime)reader["AccessTokenExpiry"],
                    RefreshTokenExpiry = (DateTime)reader["RefreshTokenExpiry"],
                    CreatedAt = (DateTime)reader["CreatedAt"]
                };
            }
            return null;
        }
    }
}
