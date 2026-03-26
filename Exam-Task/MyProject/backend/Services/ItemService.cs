using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MyProject.Application.DTOs.Item;
using MyProject.Application.Exceptions;
using MyProject.Application.Interfaces;
using MyProject.Domain.Constants;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Application.Services
{
    public class ItemService : IItemService
    {
        private readonly IQuickBooksService _quickBooksService;
        private readonly ICompanyRepository _companyRepository;
        private readonly HttpClient _httpClient;

        public ItemService(
            IQuickBooksService quickBooksService,
            ICompanyRepository companyRepository,
            IHttpClientFactory httpClientFactory)
        {
            _quickBooksService = quickBooksService;
            _companyRepository = companyRepository;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<List<ItemDto>> GetItemsAsync(string userId, string? companyId = null)
        {
            var items = new List<ItemDto>();
            var companies = await ResolveCompaniesAsync(userId, companyId);

            foreach (var company in companies)
            {
                var accessToken = await _quickBooksService.GetValidAccessTokenAsync(userId, company.Id);

                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{AppConstants.QuickBooksBaseUrl}/v3/company/{company.RealmId}/query?query=SELECT * FROM Item WHERE Active = true MAXRESULTS 1000&minorversion=65");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new ExternalServiceException($"Failed to fetch items from QuickBooks company '{company.CompanyName}'.", error);
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                if (data.TryGetProperty("QueryResponse", out var queryResponse) &&
                    queryResponse.TryGetProperty("Item", out var itemArray))
                {
                    foreach (var item in itemArray.EnumerateArray())
                    {
                        items.Add(new ItemDto
                        {
                            Id = item.GetProperty("Id").GetString()!,
                            CompanyId = company.Id,
                            CompanyName = company.CompanyName,
                            RealmId = company.RealmId,
                            Name = item.GetProperty("Name").GetString()!,
                            Description = item.TryGetProperty("Description", out var desc)
                                ? desc.GetString() : null,
                            UnitPrice = item.TryGetProperty("UnitPrice", out var price)
                                ? price.GetDecimal() : 0,
                            Type = item.GetProperty("Type").GetString()!,
                            Active = item.GetProperty("Active").GetBoolean()
                        });
                    }
                }
            }

            return items;
        }

        public async Task<ItemDto> CreateItemAsync(string userId, CreateItemDto dto, string? companyId = null)
        {
            var company = await ResolveSingleCompanyAsync(userId, companyId);
            var accessToken = await _quickBooksService.GetValidAccessTokenAsync(userId, company.Id);
            var realmId = company.RealmId;

            var payload = new
            {
                Name = dto.Name,
                Description = dto.Description,
                UnitPrice = dto.UnitPrice,
                Type = dto.Type,
                IncomeAccountRef = new { value = dto.IncomeAccountRef }
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{AppConstants.QuickBooksBaseUrl}/v3/company/{realmId}/item?minorversion=65");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("Failed to create item in QuickBooks.", error);
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            var item = data.GetProperty("Item");

            return new ItemDto
            {
                Id = item.GetProperty("Id").GetString()!,
                CompanyId = company.Id,
                CompanyName = company.CompanyName,
                RealmId = company.RealmId,
                Name = item.GetProperty("Name").GetString()!,
                Description = item.TryGetProperty("Description", out var desc) ? desc.GetString() : null,
                UnitPrice = item.TryGetProperty("UnitPrice", out var price) ? price.GetDecimal() : 0,
                Type = item.GetProperty("Type").GetString()!,
                Active = item.GetProperty("Active").GetBoolean()
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
