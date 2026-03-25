using ADO_EFcore.Dto;
using ADO_EFcore.Models;
using Microsoft.Data.SqlClient;
using System.Data;

namespace ADO_EFcore.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly string _connectionString;
        public UserRepository(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }
        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                @"SELECT Id, Name, Email, Role FROM Users WHERE Email = @Email", connection);
            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);

            if (await reader.ReadAsync())
            {
                return MapToUserDto(reader);
            }
            return null;
        }
        public async Task<Users?> GetByEmailForLoginAsync(string email)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                "SELECT Id, Name, Email, Password, Role, CreatedAt FROM Users WHERE Email = @Email",
                connection);

            command.Parameters.AddWithValue("@Email", email);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);

            if (await reader.ReadAsync())
            {
                return new Users
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Email = reader.GetString(reader.GetOrdinal("Email")),
                    Password = reader.GetString(reader.GetOrdinal("Password")),
                    Role = reader.GetString(reader.GetOrdinal("Role")),
                    CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
                };
            }

            return null;
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(
                @"SELECT Id, Name, Email, Role FROM Users WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync(CommandBehavior.SingleRow);

            if (await reader.ReadAsync())
            {
                return MapToUserDto(reader);
            }
            return null;
        }

        public async Task<List<UserDto>> GetAllAsync()
        {
            var users = new List<UserDto>();
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"SELECT Id, Name, Email, Role FROM Users", connection);

            await connection.OpenAsync();
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                users.Add(MapToUserDto(reader));
            }
            return users;
        }

        public async Task<int> CreateAsync(Users user)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                INSERT INTO Users (Name, Email, Password, Role, CreatedAt) OUTPUT INSERTED.Id
                VALUES (@Name, @Email, @Password, @Role, @CreatedAt)", connection);

            command.Parameters.AddWithValue("@Name", user.Name);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Password", user.Password);
            command.Parameters.AddWithValue("@Role", user.Role);
            command.Parameters.AddWithValue("@CreatedAt", DateTime.UtcNow);

            await connection.OpenAsync();
            return (int)await command.ExecuteScalarAsync();
        }

        public async Task<bool> UpdateAsync(Users user)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand(@"
                UPDATE Users SET Name = @Name, Email = @Email, Role = @Role WHERE Id = @Id", connection);

            command.Parameters.AddWithValue("@Id", user.Id);
            command.Parameters.AddWithValue("@Name", user.Name);
            command.Parameters.AddWithValue("@Email", user.Email);
            command.Parameters.AddWithValue("@Role", user.Role);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            using var command = new SqlCommand("DELETE FROM Users WHERE Id = @Id", connection);
            command.Parameters.AddWithValue("@Id", id);

            await connection.OpenAsync();
            return await command.ExecuteNonQueryAsync() > 0;
        }

        private static UserDto MapToUserDto(SqlDataReader reader)
        {
            return new UserDto
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Email = reader.GetString(reader.GetOrdinal("Email")),
                Role = reader.GetString(reader.GetOrdinal("Role"))
            };
        }
    }
}