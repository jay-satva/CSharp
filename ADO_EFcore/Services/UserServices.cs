using ADO_EFcore.Dto;
using ADO_EFcore.Models;
using ADO_EFcore.Repository;
using BCrypt.Net;
using Microsoft.Data.SqlClient;
using System.Diagnostics.Metrics;

namespace ADO_EFcore.Services
{
    public class UserServices
    {
        private readonly IUserRepository _userRepository;

        public UserServices(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public async Task<Users?> GetByEmailForLoginAsync(string email)
        {
            return await _userRepository.GetByEmailForLoginAsync(email);
        }
        public async Task<Users> CreateAsync(RegisterReq request)   
        {
            var user = new Users
            {
                Name = request.Name,
                Email = request.Email,
                Password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = request.Role,
                CreatedAt = DateTime.UtcNow
            };

            user.Id = await _userRepository.CreateAsync(user);
            return user;
        }

        public bool VerifyPassword(string password, string hashedPassword)
        {
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public async Task<List<UserDto>> GetAllAsync()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<UserDto?> GetByIdAsync(int id)
        {
            return await _userRepository.GetByIdAsync(id);
        }


        public async Task<bool> UpdateAsync(int id, Users updated)
        {
            var existing = await _userRepository.GetByIdAsync(id); // This now returns DTO, but we need full model for update
            if (existing == null) return false;

            // For update, we fetch full user separately or modify approach slightly.
            // For simplicity, we'll pass updated data to repository (already handled)
            var userToUpdate = new Users
            {
                Id = id,
                Name = updated.Name,
                Email = updated.Email,
                Role = updated.Role
                // Password remains unchanged
            };

            return await _userRepository.UpdateAsync(userToUpdate);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            return await _userRepository.DeleteAsync(id);
        }
    }
}