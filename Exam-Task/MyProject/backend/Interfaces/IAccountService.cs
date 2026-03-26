using MyProject.Application.DTOs.Account;

namespace MyProject.Application.Interfaces
{
    public interface IAccountService
    {
        Task<List<AccountDto>> GetAccountsAsync(string userId, string? companyId = null);
        Task<AccountDto> CreateAccountAsync(string userId, CreateAccountDto dto, string? companyId = null);
    }
}
