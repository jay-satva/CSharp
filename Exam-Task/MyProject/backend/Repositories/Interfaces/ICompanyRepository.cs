using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Repositories.Interfaces
{
    public interface ICompanyRepository
    {
        Task<Company?> GetByUserIdAsync(string userId);
        Task<Company?> GetByRealmIdAsync(string realmId);
        Task<Company?> GetByIdAsync(string id);
        Task<Company?> GetByUserAndRealmIdAsync(string userId, string realmId);
        Task<List<Company>> GetAllByUserIdAsync(string userId);
        Task<List<Company>> GetConnectedByUserIdAsync(string userId);
        Task CreateAsync(Company company);
        Task UpdateAsync(Company company);
        Task DeleteAsync(string id);
    }
}
