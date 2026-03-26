using MyProject.Application.DTOs.User;

namespace MyProject.Application.Interfaces
{
    public interface IUserService
    {
        Task<UserProfileDto> GetProfileAsync(string userId);
        Task<UserProfileDto> UpdateProfileAsync(string userId, UpdateUserProfileDto dto);
    }
}
