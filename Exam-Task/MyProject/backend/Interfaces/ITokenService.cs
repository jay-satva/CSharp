using MyProject.Domain.Entities;

namespace MyProject.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(User user);
        RefreshToken GenerateRefreshToken();
        string? GetUserIdFromToken(string token);
    }
}