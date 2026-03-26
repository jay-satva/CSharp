using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(string id);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByIntuitSubIdAsync(string intuitSubId);
        Task<User?> FindByRefreshTokenAsync(string token);
        Task CreateAsync(User user);
        Task UpdateAsync(User user);
    }
}
