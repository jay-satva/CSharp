using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MyProject.Application.DTOs.Account;
using MyProject.Application.Exceptions;
using MyProject.Application.Interfaces;
using MyProject.Domain.Constants;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Application.Services
{
    public class AccountService : IAccountService
    {
        private readonly IQuickBooksService _quickBooksService;
        private readonly ICompanyRepository _companyRepository;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AccountService(
            IQuickBooksService quickBooksService,
            ICompanyRepository companyRepository,
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _quickBooksService = quickBooksService;
            _companyRepository = companyRepository;
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<List<AccountDto>> GetAccountsAsync(string userId, string? companyId = null)
        {
            var accounts = new List<AccountDto>();
            var companies = await ResolveCompaniesAsync(userId, companyId);

            foreach (var company in companies)
            {
                var accessToken = await _quickBooksService.GetValidAccessTokenAsync(userId, company.Id);
                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{AppConstants.QuickBooksBaseUrl}/v3/company/{company.RealmId}/query?query=SELECT * FROM Account WHERE Active = true MAXRESULTS 1000&minorversion=65");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new ExternalServiceException($"Failed to fetch accounts from QuickBooks company '{company.CompanyName}'.", error);
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                if (data.TryGetProperty("QueryResponse", out var queryResponse) &&
                    queryResponse.TryGetProperty("Account", out var accountArray))
                {
                    foreach (var account in accountArray.EnumerateArray())
                    {
                        accounts.Add(new AccountDto
                        {
                            Id = account.GetProperty("Id").GetString()!,
                            CompanyId = company.Id,
                            CompanyName = company.CompanyName,
                            RealmId = company.RealmId,
                            Name = account.GetProperty("Name").GetString()!,
                            AccountType = account.GetProperty("AccountType").GetString()!,
                            AccountSubType = account.TryGetProperty("AccountSubType", out var subType)
                                ? subType.GetString() : null,
                            Description = account.TryGetProperty("Description", out var desc)
                                ? desc.GetString() : null,
                            Active = account.GetProperty("Active").GetBoolean()
                        });
                    }
                }
            }

            return accounts;
        }

        public async Task<AccountDto> CreateAccountAsync(string userId, CreateAccountDto dto, string? companyId = null)
        {
            var company = await ResolveSingleCompanyAsync(userId, companyId);
            var accessToken = await _quickBooksService.GetValidAccessTokenAsync(userId, company.Id);
            var realmId = company.RealmId;

            var payload = new
            {
                Name = dto.Name,
                AccountType = dto.AccountType,
                AccountSubType = dto.AccountSubType,
                Description = dto.Description
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{AppConstants.QuickBooksBaseUrl}/v3/company/{realmId}/account?minorversion=65");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("Failed to create account in QuickBooks.", error);
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            var account = data.GetProperty("Account");

            return new AccountDto
            {
                Id = account.GetProperty("Id").GetString()!,
                CompanyId = company.Id,
                CompanyName = company.CompanyName,
                RealmId = company.RealmId,
                Name = account.GetProperty("Name").GetString()!,
                AccountType = account.GetProperty("AccountType").GetString()!,
                AccountSubType = account.TryGetProperty("AccountSubType", out var subType)
                    ? subType.GetString() : null,
                Description = account.TryGetProperty("Description", out var desc)
                    ? desc.GetString() : null,
                Active = account.GetProperty("Active").GetBoolean()
            };
        }

        private async Task<List<Domain.Entities.Company>> ResolveCompaniesAsync(string userId, string? companyId)
        {
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                var single = await ResolveSingleCompanyAsync(userId, companyId);
                return new List<Domain.Entities.Company> { single };
            }

            var connected = await _companyRepository.GetConnectedByUserIdAsync(userId);
            if (connected.Count == 0)
                throw new BadRequestException("No connected QuickBooks company found.");

            return connected;
        }

        private async Task<Domain.Entities.Company> ResolveSingleCompanyAsync(string userId, string? companyId)
        {
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                var selected = await _companyRepository.GetByIdAsync(companyId);
                if (selected != null && selected.UserId == userId && selected.IsConnected)
                    return selected;

                throw new BadRequestException("Selected QuickBooks company is not connected.");
            }

            var fallback = await _companyRepository.GetByUserIdAsync(userId);
            if (fallback == null || !fallback.IsConnected)
                throw new BadRequestException("No connected QuickBooks company found.");

            return fallback;
        }
    }
}
