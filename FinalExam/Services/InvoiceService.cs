using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FinalExam.Data;
using FinalExam.DTOs;
using FinalExam.Models;

namespace FinalExam.Services;

public class InvoiceService
{
    private readonly QuickBooksService _quickBooksService;
    private readonly CompanyRepository _companyRepository;
    private readonly InvoiceRepository _invoiceRepository;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public InvoiceService(
        QuickBooksService quickBooksService,
        CompanyRepository companyRepository,
        InvoiceRepository invoiceRepository,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _quickBooksService = quickBooksService;
        _companyRepository = companyRepository;
        _invoiceRepository = invoiceRepository;
        _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
    }

    public async Task<List<InvoiceDto>> GetInvoicesAsync(string userId)
    {
        var invoices = await _invoiceRepository.GetAllByUserIdAsync(userId);
        var companies = await _companyRepository.GetByUserIdAsync(userId);
        var companyByRealm = companies
            .GroupBy(company => company.RealmId)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        return invoices
            .Where(invoice => companyByRealm.ContainsKey(invoice.RealmId))
            .Select(invoice => MapInvoice(invoice, companyByRealm[invoice.RealmId].CompanyName))
            .ToList();
    }

    public async Task<InvoiceDto> GetInvoiceByIdAsync(string userId, int id)
    {
        var invoice = await _invoiceRepository.GetByIdAsync(id, userId)
            ?? throw new Exception("Invoice not found.");

        var company = await _companyRepository.GetByUserIdAndRealmIdAsync(userId, invoice.RealmId)
            ?? throw new Exception("Connected company not found for this invoice.");

        return MapInvoice(invoice, company.CompanyName);
    }

    public async Task<InvoiceDto> CreateInvoiceAsync(string userId, CreateInvoiceRequest requestDto)
    {
        var company = await ResolveCompanyAsync(userId, requestDto.RealmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var quickBooksInvoice = await CreateInvoiceInQuickBooksAsync(accessToken, company.RealmId, requestDto, null);

        var invoiceRecord = new InvoiceRecord
        {
            UserId = userId,
            RealmId = company.RealmId,
            QuickBooksInvoiceId = ReadString(quickBooksInvoice, "Id") ?? throw new Exception("QuickBooks invoice id was not returned."),
            CustomerRef = requestDto.CustomerRef.Trim(),
            CustomerName = requestDto.CustomerName.Trim(),
            InvoiceDate = requestDto.InvoiceDate,
            DueDate = requestDto.DueDate,
            Memo = NormalizeNullable(requestDto.Memo),
            TotalAmount = requestDto.LineItems.Sum(lineItem => lineItem.Quantity * lineItem.UnitPrice),
            Status = ResolveInvoiceStatus(quickBooksInvoice),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            LineItems = requestDto.LineItems.Select(MapLineItem).ToList()
        };

        var created = await _invoiceRepository.CreateAsync(invoiceRecord);
        return MapInvoice(created, company.CompanyName);
    }

    public async Task<InvoiceDto> UpdateInvoiceAsync(string userId, int id, UpdateInvoiceRequest requestDto)
    {
        var existing = await _invoiceRepository.GetByIdAsync(id, userId)
            ?? throw new Exception("Invoice not found.");

        var company = await ResolveCompanyAsync(userId, existing.RealmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var existingQuickBooksInvoice = await GetQuickBooksInvoiceAsync(accessToken, company.RealmId, existing.QuickBooksInvoiceId);
        var syncToken = ReadString(existingQuickBooksInvoice, "SyncToken");
        if (string.IsNullOrWhiteSpace(syncToken))
            throw new Exception("QuickBooks SyncToken was not returned for this invoice.");

        var updatedQuickBooksInvoice = await CreateInvoiceInQuickBooksAsync(
            accessToken,
            company.RealmId,
            new CreateInvoiceRequest
            {
                RealmId = existing.RealmId,
                CustomerRef = requestDto.CustomerRef,
                CustomerName = requestDto.CustomerName,
                AccountRef = existingQuickBooksInvoice.TryGetProperty("ARAccountRef", out var arAccountRef)
                    ? ReadString(arAccountRef, "value")
                    : null,
                InvoiceDate = requestDto.InvoiceDate,
                DueDate = requestDto.DueDate,
                Memo = requestDto.Memo,
                LineItems = requestDto.LineItems
            },
            new QuickBooksInvoiceUpdateContext
            {
                InvoiceId = existing.QuickBooksInvoiceId,
                SyncToken = syncToken
            });

        existing.CustomerRef = requestDto.CustomerRef.Trim();
        existing.CustomerName = requestDto.CustomerName.Trim();
        existing.InvoiceDate = requestDto.InvoiceDate;
        existing.DueDate = requestDto.DueDate;
        existing.Memo = NormalizeNullable(requestDto.Memo);
        existing.TotalAmount = requestDto.LineItems.Sum(lineItem => lineItem.Quantity * lineItem.UnitPrice);
        existing.Status = ResolveInvoiceStatus(updatedQuickBooksInvoice);
        existing.UpdatedAt = DateTime.UtcNow;
        existing.LineItems = requestDto.LineItems.Select(MapLineItem).ToList();

        await _invoiceRepository.UpdateAsync(existing);
        return MapInvoice(existing, company.CompanyName);
    }

    public async Task DeleteInvoiceAsync(string userId, int id)
    {
        var existing = await _invoiceRepository.GetByIdAsync(id, userId)
            ?? throw new Exception("Invoice not found.");

        var company = await ResolveCompanyAsync(userId, existing.RealmId);
        var accessToken = await _quickBooksService.GetAccessTokenAsync(userId, company.RealmId);
        var existingQuickBooksInvoice = await GetQuickBooksInvoiceAsync(accessToken, company.RealmId, existing.QuickBooksInvoiceId);
        var syncToken = ReadString(existingQuickBooksInvoice, "SyncToken");
        if (string.IsNullOrWhiteSpace(syncToken))
            throw new Exception("QuickBooks SyncToken was not returned for this invoice.");

        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";
        var payload = new Dictionary<string, object?>
        {
            ["Id"] = existing.QuickBooksInvoiceId,
            ["SyncToken"] = syncToken
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"{baseUrl}/v3/company/{company.RealmId}/invoice?operation=delete&minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to delete invoice from QuickBooks: {error}");
        }

        await _invoiceRepository.DeleteAsync(id, userId);
    }

    private async Task<JsonElement> CreateInvoiceInQuickBooksAsync(
        string accessToken,
        string realmId,
        CreateInvoiceRequest requestDto,
        QuickBooksInvoiceUpdateContext? updateContext)
    {
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";
        var payload = new Dictionary<string, object?>
        {
            ["CustomerRef"] = new
            {
                value = requestDto.CustomerRef.Trim(),
                name = requestDto.CustomerName.Trim()
            },
            ["TxnDate"] = requestDto.InvoiceDate.ToString("yyyy-MM-dd"),
            ["Line"] = requestDto.LineItems.Select(lineItem => new Dictionary<string, object?>
            {
                ["Amount"] = lineItem.Quantity * lineItem.UnitPrice,
                ["Description"] = NormalizeNullable(lineItem.Description),
                ["DetailType"] = "SalesItemLineDetail",
                ["SalesItemLineDetail"] = new
                {
                    ItemRef = new
                    {
                        value = lineItem.ItemRef.Trim(),
                        name = lineItem.ItemName.Trim()
                    },
                    Qty = lineItem.Quantity,
                    UnitPrice = lineItem.UnitPrice
                }
            }).ToList()
        };

        if (requestDto.DueDate.HasValue)
            payload["DueDate"] = requestDto.DueDate.Value.ToString("yyyy-MM-dd");

        if (!string.IsNullOrWhiteSpace(requestDto.Memo))
            payload["PrivateNote"] = requestDto.Memo.Trim();

        if (!string.IsNullOrWhiteSpace(requestDto.AccountRef))
            payload["ARAccountRef"] = new { value = requestDto.AccountRef.Trim() };

        var url = $"{baseUrl}/v3/company/{realmId}/invoice?minorversion=65";
        if (updateContext != null)
        {
            payload["Id"] = updateContext.InvoiceId;
            payload["SyncToken"] = updateContext.SyncToken;
            url = $"{baseUrl}/v3/company/{realmId}/invoice?operation=update&minorversion=65";
        }

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to {(updateContext == null ? "create" : "update")} invoice in QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("Invoice").Clone();
    }

    private async Task<JsonElement> GetQuickBooksInvoiceAsync(string accessToken, string realmId, string invoiceId)
    {
        var baseUrl = _configuration["QuickBooks:BaseUrl"] ?? "https://sandbox-quickbooks.api.intuit.com";
        var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{baseUrl}/v3/company/{realmId}/invoice/{invoiceId}?minorversion=65");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to fetch invoice from QuickBooks: {error}");
        }

        var json = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(json);
        return document.RootElement.GetProperty("Invoice").Clone();
    }

    private async Task<Company> ResolveCompanyAsync(string userId, string realmId)
    {
        var company = await _companyRepository.GetActiveByUserIdAndRealmIdAsync(userId, realmId);
        if (company == null)
            throw new Exception("Selected company is not connected.");

        return company;
    }

    private static InvoiceLineItemRecord MapLineItem(CreateInvoiceLineItemRequest lineItem)
    {
        return new InvoiceLineItemRecord
        {
            ItemRef = lineItem.ItemRef.Trim(),
            ItemName = lineItem.ItemName.Trim(),
            Description = NormalizeNullable(lineItem.Description),
            Quantity = lineItem.Quantity,
            UnitPrice = lineItem.UnitPrice,
            Amount = lineItem.Quantity * lineItem.UnitPrice
        };
    }

    private static InvoiceDto MapInvoice(InvoiceRecord invoice, string companyName)
    {
        return new InvoiceDto
        {
            Id = invoice.Id,
            RealmId = invoice.RealmId,
            CompanyName = companyName,
            QuickBooksInvoiceId = invoice.QuickBooksInvoiceId,
            CustomerRef = invoice.CustomerRef,
            CustomerName = invoice.CustomerName,
            InvoiceDate = invoice.InvoiceDate,
            DueDate = invoice.DueDate,
            Memo = invoice.Memo,
            TotalAmount = invoice.TotalAmount,
            Status = invoice.Status,
            CreatedAt = invoice.CreatedAt,
            UpdatedAt = invoice.UpdatedAt,
            LineItems = invoice.LineItems.Select(lineItem => new InvoiceLineItemDto
            {
                Id = lineItem.Id,
                ItemRef = lineItem.ItemRef,
                ItemName = lineItem.ItemName,
                Description = lineItem.Description,
                Quantity = lineItem.Quantity,
                UnitPrice = lineItem.UnitPrice,
                Amount = lineItem.Amount
            }).ToList()
        };
    }

    private static string ResolveInvoiceStatus(JsonElement invoice)
    {
        if (invoice.TryGetProperty("Balance", out var balanceElement) &&
            balanceElement.ValueKind == JsonValueKind.Number &&
            balanceElement.GetDecimal() == 0)
        {
            return "Paid";
        }

        var emailStatus = ReadString(invoice, "EmailStatus");
        if (!string.IsNullOrWhiteSpace(emailStatus) &&
            !string.Equals(emailStatus, "NotSet", StringComparison.OrdinalIgnoreCase))
        {
            return emailStatus;
        }

        return "Draft";
    }

    private static string? NormalizeNullable(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? ReadString(JsonElement source, string propertyName)
    {
        if (!source.TryGetProperty(propertyName, out var property) || property.ValueKind == JsonValueKind.Null)
            return null;

        var value = property.GetString();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private sealed class QuickBooksInvoiceUpdateContext
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string SyncToken { get; set; } = string.Empty;
    }
}
