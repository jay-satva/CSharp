using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinalExam.Data;
using FinalExam.DTOs;

namespace FinalExam.Services;

public class AccountService
{
    private readonly QuickBooksService _quickBooksService;
    private readonly CompanyRepository _companyRepository;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public AccountService(
        QuickBooksService quickBooksService,
        CompanyRepository companyRepository,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _quickBooksService = quickBooksService;
        _companyRepository = companyRepository;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<List<AccountDto>> GetAccountsAsync(string userId, string? realmId)
    {
        var company = await ResolveCompanyAsync(userId, realmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";
        var query = Uri.EscapeDataString("SELECT * FROM Account MAXRESULTS 1000");

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/v3/company/{company.RealmId}/query?query={query}&minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch accounts from QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var accounts = new List<AccountDto>();

        if (!doc.RootElement.TryGetProperty("QueryResponse", out var queryResponse) ||
            !queryResponse.TryGetProperty("Account", out var accountArray))
            return accounts;

        foreach (var account in accountArray.EnumerateArray())
        {
            accounts.Add(new AccountDto
            {
                Id = ReadString(account, "Id") ?? string.Empty,
                RealmId = company.RealmId,
                CompanyName = company.CompanyName,
                Name = ReadString(account, "Name") ?? string.Empty,
                AcctNum = ReadString(account, "AcctNum"),
                AccountType = ReadString(account, "AccountType") ?? string.Empty,
                AccountSubType = ReadString(account, "AccountSubType"),
                Description = ReadString(account, "Description"),
                Classification = ReadString(account, "Classification"),
                Active = account.TryGetProperty("Active", out var active) && active.ValueKind == JsonValueKind.True
            });
        }

        return accounts
            .OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<AccountDto> CreateAccountAsync(string userId, CreateAccountRequest requestDto)
    {
        var company = await ResolveCompanyAsync(userId, requestDto.RealmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";

        var payload = new Dictionary<string, object?>
        {
            ["Name"] = requestDto.Name.Trim(),
            ["AccountType"] = requestDto.AccountType.Trim()
        };

        if (!string.IsNullOrWhiteSpace(requestDto.AcctNum))
            payload["AcctNum"] = requestDto.AcctNum.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.AccountSubType))
            payload["AccountSubType"] = requestDto.AccountSubType.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.Description))
            payload["Description"] = requestDto.Description.Trim();

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/v3/company/{company.RealmId}/account?minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create account in QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var account = doc.RootElement.GetProperty("Account");

        return new AccountDto
        {
            Id = ReadString(account, "Id") ?? string.Empty,
            RealmId = company.RealmId,
            CompanyName = company.CompanyName,
            Name = ReadString(account, "Name") ?? requestDto.Name.Trim(),
            AcctNum = ReadString(account, "AcctNum"),
            AccountType = ReadString(account, "AccountType") ?? requestDto.AccountType.Trim(),
            AccountSubType = ReadString(account, "AccountSubType"),
            Description = ReadString(account, "Description"),
            Classification = ReadString(account, "Classification"),
            Active = account.TryGetProperty("Active", out var active) && active.ValueKind == JsonValueKind.True
        };
    }

    private async Task<Models.Company> ResolveCompanyAsync(string userId, string? realmId)
    {
        if (!string.IsNullOrWhiteSpace(realmId))
        {
            var selected = await _companyRepository.GetActiveByUserIdAndRealmIdAsync(userId, realmId);
            if (selected != null)
                return selected;

            throw new Exception("Selected company is not connected.");
        }

        var fallback = (await _companyRepository.GetActiveByUserIdAsync(userId)).FirstOrDefault();
        if (fallback == null)
            throw new Exception("No connected company found.");

        return fallback;
    }

    private static string? ReadString(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
