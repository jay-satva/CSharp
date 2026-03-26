using MyProject.Application.DTOs.Auth;

namespace MyProject.Application.Interfaces
{
    public interface IAuthService
    {
        Task<AuthResponseDto> SignUpAsync(SignUpDto dto);
        Task<AuthResponseDto> SignInAsync(SignInDto dto);
        Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
        Task RevokeTokenAsync(string refreshToken, string userId);
        Task<AuthResponseDto> IntuitCallbackAsync(string code, string state);
    }
}