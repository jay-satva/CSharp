using MyProject.Application.DTOs.User;
using MyProject.Application.Exceptions;
using MyProject.Application.Interfaces;
using MyProject.Domain.Entities;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Application.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User account was not found.");

            return MapProfile(user);
        }

        public async Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateUserProfileDto dto)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new NotFoundException("User account was not found.");

            var normalizedEmail = dto.Email.Trim().ToLower();
            if (!string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
            {
                if (user.AuthProvider == "intuit")
                    throw new BadRequestException("Email cannot be changed for Intuit users.");

                var existing = await _userRepository.GetByEmailAsync(normalizedEmail);
                if (existing != null && existing.Id != user.Id)
                    throw new ConflictException("This email is already registered.");
            }

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            user.Email = normalizedEmail;
            user.PhoneNumber = NormalizeNullable(dto.PhoneNumber);
            user.ProfilePhotoUrl = NormalizeNullable(dto.ProfilePhotoUrl);

            if (!string.IsNullOrWhiteSpace(dto.NewPassword))
            {
                if (user.AuthProvider != "manual")
                    throw new BadRequestException("Password changes are available only for manual users.");

                if (string.IsNullOrWhiteSpace(user.PasswordHash) || !BCrypt.Net.BCrypt.Verify(dto.CurrentPassword ?? string.Empty, user.PasswordHash))
                    throw new UnauthorizedException("Current password is incorrect.");

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            }

            user.UpdatedAt = DateTime.UtcNow;
            await _userRepository.UpdateAsync(user);
            return MapProfile(user);
        }

        private static string? NormalizeNullable(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static UserProfileDto MapProfile(User user)
        {
            var incompleteFields = new List<string>();
            if (string.IsNullOrWhiteSpace(user.PhoneNumber))
                incompleteFields.Add("phoneNumber");
            if (string.IsNullOrWhiteSpace(user.ProfilePhotoUrl))
                incompleteFields.Add("profilePhotoUrl");

            return new UserProfileDto
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AuthProvider = user.AuthProvider,
                PhoneNumber = user.PhoneNumber,
                ProfilePhotoUrl = user.ProfilePhotoUrl,
                IncompleteFields = incompleteFields
            };
        }
    }
}
