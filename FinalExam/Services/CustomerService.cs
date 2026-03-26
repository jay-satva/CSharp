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

        var payload = BuildUpsertPayload(requestDto);

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

        var payload = BuildUpsertPayload(requestDto);
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

    private static Dictionary<string, object?> BuildUpsertPayload(CreateCustomerRequest requestDto)
    {
        var payload = new Dictionary<string, object?>();

        if (!string.IsNullOrWhiteSpace(requestDto.DisplayName))
            payload["DisplayName"] = requestDto.DisplayName.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.Title))
            payload["Title"] = requestDto.Title.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.GivenName))
            payload["GivenName"] = requestDto.GivenName.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.MiddleName))
            payload["MiddleName"] = requestDto.MiddleName.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.FamilyName))
            payload["FamilyName"] = requestDto.FamilyName.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.Suffix))
            payload["Suffix"] = requestDto.Suffix.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.CompanyName))
            payload["CompanyName"] = requestDto.CompanyName.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.Email))
            payload["PrimaryEmailAddr"] = new { Address = requestDto.Email.Trim() };

        if (!string.IsNullOrWhiteSpace(requestDto.Phone))
            payload["PrimaryPhone"] = new { FreeFormNumber = requestDto.Phone.Trim() };

        if (!string.IsNullOrWhiteSpace(requestDto.Mobile))
            payload["Mobile"] = new { FreeFormNumber = requestDto.Mobile.Trim() };

        if (!string.IsNullOrWhiteSpace(requestDto.BillAddrLine1) ||
            !string.IsNullOrWhiteSpace(requestDto.BillAddrCity) ||
            !string.IsNullOrWhiteSpace(requestDto.BillAddrPostalCode) ||
            !string.IsNullOrWhiteSpace(requestDto.BillAddrCountrySubDivisionCode))
        {
            payload["BillAddr"] = new
            {
                Line1 = requestDto.BillAddrLine1?.Trim(),
                City = requestDto.BillAddrCity?.Trim(),
                PostalCode = requestDto.BillAddrPostalCode?.Trim(),
                CountrySubDivisionCode = requestDto.BillAddrCountrySubDivisionCode?.Trim()
            };
        }

        return payload;
    }

    private static CustomerDto MapCustomer(JsonElement customer, string realmId, string companyName)
    {
        var hasBillAddr = customer.TryGetProperty("BillAddr", out var billAddrObj);

        return new CustomerDto
        {
            Id = ReadString(customer, "Id") ?? string.Empty,
            SyncToken = ReadString(customer, "SyncToken") ?? string.Empty,
            RealmId = realmId,
            ConnectedCompanyName = companyName,
            DisplayName = ReadString(customer, "DisplayName") ?? string.Empty,
            GivenName = ReadString(customer, "GivenName"),
            MiddleName = ReadString(customer, "MiddleName"),
            FamilyName = ReadString(customer, "FamilyName"),
            Title = ReadString(customer, "Title"),
            Suffix = ReadString(customer, "Suffix"),
            CustomerCompanyName = ReadString(customer, "CompanyName"),
            Email = customer.TryGetProperty("PrimaryEmailAddr", out var emailObj)
                ? ReadString(emailObj, "Address")
                : null,
            Phone = customer.TryGetProperty("PrimaryPhone", out var phoneObj)
                ? ReadString(phoneObj, "FreeFormNumber")
                : null,
            Mobile = customer.TryGetProperty("Mobile", out var mobileObj)
                ? ReadString(mobileObj, "FreeFormNumber")
                : null,
            BillAddrLine1 = hasBillAddr
                ? ReadString(billAddrObj, "Line1")
                : null,
            BillAddrCity = hasBillAddr
                ? ReadString(billAddrObj, "City")
                : null,
            BillAddrPostalCode = hasBillAddr
                ? ReadString(billAddrObj, "PostalCode")
                : null,
            BillAddrCountrySubDivisionCode = hasBillAddr
                ? ReadString(billAddrObj, "CountrySubDivisionCode")
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
