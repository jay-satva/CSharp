using Microsoft.Data.SqlClient;
using AccountingCRUD.Models;
using System.Data;

namespace AccountingCRUD.Repository
{
    public class TokenRepository
    {
        private readonly string _connectionString;

        public TokenRepository(IConfiguration config)
        {
            _connectionString = config.GetConnectionString("DefaultConnection");
        }

        public async Task<QuickBooksToken> GetTokenByUserIdAsync(string userId)
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
