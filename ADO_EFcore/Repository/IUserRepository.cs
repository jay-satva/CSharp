using ADO_EFcore.Models;
using ADO_EFcore.Dto;
namespace ADO_EFcore.Repository
{
    public interface IUserRepository
    {
        Task<UserDto?> GetByEmailAsync(string email);
        Task<UserDto?> GetByIdAsync(int id);
        Task<List<UserDto>> GetAllAsync();
        Task<int> CreateAsync(Users user);
        Task<bool> UpdateAsync(Users user);
        Task<bool> DeleteAsync(int id);
        Task<Users?> GetByEmailForLoginAsync(string email);
    }
}