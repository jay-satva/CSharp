using MyProject.Application.DTOs.QuickBooks;

namespace MyProject.Application.Interfaces
{
    public interface IQuickBooksService
    {
        string GetAuthorizationUrl(string userId);
        Task<CompanyDto> HandleCallbackAsync(string code, string realmId, string userId);
        Task DisconnectAsync(string userId, string companyId);
        Task<CompanyDto?> GetConnectedCompanyAsync(string userId, string? companyId = null);
        Task<List<CompanyDto>> GetConnectedCompaniesAsync(string userId);
        Task<string> GetValidAccessTokenAsync(string userId, string? companyId = null);
    }
}
