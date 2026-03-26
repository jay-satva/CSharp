using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinalExam.Data;
using FinalExam.DTOs;

namespace FinalExam.Services;

public class ItemService
{
    private readonly QuickBooksService _quickBooksService;
    private readonly CompanyRepository _companyRepository;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public ItemService(
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

    public async Task<List<ItemDto>> GetItemsAsync(string userId, string? realmId)
    {
        var company = await ResolveCompanyAsync(userId, realmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";
        var query = Uri.EscapeDataString("SELECT * FROM Item WHERE Active = true MAXRESULTS 1000");

        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/v3/company/{company.RealmId}/query?query={query}&minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch items from QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var items = new List<ItemDto>();

        if (!doc.RootElement.TryGetProperty("QueryResponse", out var queryResponse) ||
            !queryResponse.TryGetProperty("Item", out var itemArray))
            return items;

        foreach (var item in itemArray.EnumerateArray())
        {
            items.Add(MapItem(item, company.RealmId, company.CompanyName));
        }

        return items
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<ItemDto> CreateItemAsync(string userId, CreateItemRequest requestDto)
    {
        var company = await ResolveCompanyAsync(userId, requestDto.RealmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";

        var payload = BuildUpsertPayload(
            requestDto.Name,
            requestDto.QtyOnHand,
            requestDto.Type,
            requestDto.IncomeAccountRef,
            requestDto.ExpenseAccountRef,
            requestDto.AssetAccountRef);

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/v3/company/{company.RealmId}/item?minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create item in QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return MapItem(doc.RootElement.GetProperty("Item"), company.RealmId, company.CompanyName);
    }

    public async Task<ItemDto> UpdateItemAsync(string userId, string itemId, UpdateItemRequest requestDto)
    {
        var company = await ResolveCompanyAsync(userId, requestDto.RealmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";

        var payload = BuildUpsertPayload(
            requestDto.Name,
            requestDto.QtyOnHand,
            requestDto.Type,
            requestDto.IncomeAccountRef,
            requestDto.ExpenseAccountRef,
            requestDto.AssetAccountRef);
        payload["Id"] = itemId;
        payload["SyncToken"] = requestDto.SyncToken.Trim();
        payload["sparse"] = true;

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/v3/company/{company.RealmId}/item?operation=update&minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to update item in QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        return MapItem(doc.RootElement.GetProperty("Item"), company.RealmId, company.CompanyName);
    }

    public async Task DeleteItemAsync(string userId, string realmId, string itemId, string syncToken)
    {
        var company = await ResolveCompanyAsync(userId, realmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";

        var payload = new Dictionary<string, object?>
        {
            ["Id"] = itemId,
            ["SyncToken"] = syncToken.Trim(),
            ["Active"] = false,
            ["sparse"] = true
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/v3/company/{company.RealmId}/item?operation=update&minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete item in QuickBooks: {error}");
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
        string name,
        decimal? qtyOnHand,
        string? type,
        string? incomeAccountRef,
        string? expenseAccountRef,
        string? assetAccountRef)
    {
        var payload = new Dictionary<string, object?>
        {
            ["Name"] = name.Trim()
        };

        if (!string.IsNullOrWhiteSpace(type))
            payload["Type"] = type.Trim();

        if (!string.IsNullOrWhiteSpace(incomeAccountRef))
            payload["IncomeAccountRef"] = new { value = incomeAccountRef.Trim() };

        var normalizedType = type?.Trim();
        if (string.Equals(normalizedType, "Inventory", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrWhiteSpace(expenseAccountRef))
                payload["ExpenseAccountRef"] = new { value = expenseAccountRef.Trim() };

            if (!string.IsNullOrWhiteSpace(assetAccountRef))
                payload["AssetAccountRef"] = new { value = assetAccountRef.Trim() };

            payload["TrackQtyOnHand"] = true;
            payload["InvStartDate"] = DateTime.UtcNow.ToString("yyyy-MM-dd");

            if (qtyOnHand.HasValue)
                payload["QtyOnHand"] = qtyOnHand.Value;
        }
        else if (qtyOnHand.HasValue)
        {
            payload["QtyOnHand"] = qtyOnHand.Value;
        }

        return payload;
    }

    private static ItemDto MapItem(JsonElement item, string realmId, string companyName)
    {
        decimal? qtyOnHand = null;
        if (item.TryGetProperty("QtyOnHand", out var qty) && qty.ValueKind == JsonValueKind.Number)
            qtyOnHand = qty.GetDecimal();

        return new ItemDto
        {
            Id = ReadString(item, "Id") ?? string.Empty,
            SyncToken = ReadString(item, "SyncToken") ?? string.Empty,
            RealmId = realmId,
            CompanyName = companyName,
            Name = ReadString(item, "Name") ?? string.Empty,
            QtyOnHand = qtyOnHand,
            Type = ReadString(item, "Type"),
            IncomeAccountName = item.TryGetProperty("IncomeAccountRef", out var incomeAccountRef)
                ? ReadString(incomeAccountRef, "name")
                : null,
            Active = !item.TryGetProperty("Active", out var active) || active.ValueKind == JsonValueKind.True
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
