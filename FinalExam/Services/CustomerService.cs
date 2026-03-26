using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinalExam.Data;
using FinalExam.DTOs;

namespace FinalExam.Services;

public class CustomerService
{
    private readonly QuickBooksService _quickBooksService;
    private readonly CompanyRepository _companyRepository;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public CustomerService(
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

    public async Task<List<CustomerDto>> GetCustomersAsync(string userId, string? realmId)
    {
        var company = await ResolveCompanyAsync(userId, realmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";
        var query = Uri.EscapeDataString("SELECT * FROM Customer WHERE Active = true MAXRESULTS 1000");

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/v3/company/{company.RealmId}/query?query={query}&minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch customers from QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var customers = new List<CustomerDto>();

        if (!doc.RootElement.TryGetProperty("QueryResponse", out var queryResponse) ||
            !queryResponse.TryGetProperty("Customer", out var customerArray))
            return customers;

        foreach (var customer in customerArray.EnumerateArray())
        {
            customers.Add(MapCustomer(customer, company.RealmId, company.CompanyName));
        }

        return customers
            .OrderBy(c => c.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<CustomerDto> CreateCustomerAsync(string userId, CreateCustomerRequest requestDto)
    {
        var company = await ResolveCompanyAsync(userId, requestDto.RealmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";

        var payload = BuildUpsertPayload(requestDto.DisplayName, requestDto.GivenName, requestDto.FamilyName, requestDto.Email, requestDto.Phone);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/v3/company/{company.RealmId}/customer?minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create customer in QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return MapCustomer(doc.RootElement.GetProperty("Customer"), company.RealmId, company.CompanyName);
    }

    public async Task<CustomerDto> UpdateCustomerAsync(string userId, string customerId, UpdateCustomerRequest requestDto)
    {
        var company = await ResolveCompanyAsync(userId, requestDto.RealmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";

        var payload = BuildUpsertPayload(requestDto.DisplayName, requestDto.GivenName, requestDto.FamilyName, requestDto.Email, requestDto.Phone);
        payload["Id"] = customerId;
        payload["SyncToken"] = requestDto.SyncToken.Trim();
        payload["sparse"] = true;

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/v3/company/{company.RealmId}/customer?operation=update&minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to update customer in QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return MapCustomer(doc.RootElement.GetProperty("Customer"), company.RealmId, company.CompanyName);
    }

    public async Task DeleteCustomerAsync(string userId, string realmId, string customerId, string syncToken)
    {
        var company = await ResolveCompanyAsync(userId, realmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";

        var payload = new Dictionary<string, object?>
        {
            ["Id"] = customerId,
            ["SyncToken"] = syncToken.Trim(),
            ["Active"] = false,
            ["sparse"] = true
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/v3/company/{company.RealmId}/customer?operation=update&minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete customer in QuickBooks: {error}");
        }
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

    private static Dictionary<string, object?> BuildUpsertPayload(
        string? displayName,
        string? givenName,
        string? familyName,
        string? email,
        string? phone)
    {
        var payload = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(displayName))
            payload["DisplayName"] = displayName.Trim();

        if (!string.IsNullOrWhiteSpace(givenName))
            payload["GivenName"] = givenName.Trim();

        if (!string.IsNullOrWhiteSpace(familyName))
            payload["FamilyName"] = familyName.Trim();

        if (!string.IsNullOrWhiteSpace(email))
            payload["PrimaryEmailAddr"] = new { Address = email.Trim() };

        if (!string.IsNullOrWhiteSpace(phone))
            payload["PrimaryPhone"] = new { FreeFormNumber = phone.Trim() };

        return payload;
    }

    private static CustomerDto MapCustomer(JsonElement customer, string realmId, string companyName)
    {
        return new CustomerDto
        {
            Id = ReadString(customer, "Id") ?? string.Empty,
            SyncToken = ReadString(customer, "SyncToken") ?? string.Empty,
            RealmId = realmId,
            CompanyName = companyName,
            DisplayName = ReadString(customer, "DisplayName") ?? string.Empty,
            GivenName = ReadString(customer, "GivenName"),
            FamilyName = ReadString(customer, "FamilyName"),
            Email = customer.TryGetProperty("PrimaryEmailAddr", out var emailObj)
                ? ReadString(emailObj, "Address")
                : null,
            Phone = customer.TryGetProperty("PrimaryPhone", out var phoneObj)
                ? ReadString(phoneObj, "FreeFormNumber")
                : null,
            Active = !customer.TryGetProperty("Active", out var active) || active.ValueKind == JsonValueKind.True
        };
    }

    private static string? ReadString(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }
}
