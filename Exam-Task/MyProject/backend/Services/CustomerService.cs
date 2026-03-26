using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using MyProject.Application.DTOs.Customer;
using MyProject.Application.Exceptions;
using MyProject.Application.Interfaces;
using MyProject.Domain.Constants;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Application.Services
{
    public class CustomerService : ICustomerService
    {
        private readonly IQuickBooksService _quickBooksService;
        private readonly ICompanyRepository _companyRepository;
        private readonly HttpClient _httpClient;

        public CustomerService(
            IQuickBooksService quickBooksService,
            ICompanyRepository companyRepository,
            IHttpClientFactory httpClientFactory)
        {
            _quickBooksService = quickBooksService;
            _companyRepository = companyRepository;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<List<CustomerDto>> GetCustomersAsync(string userId, string? companyId = null)
        {
            var customers = new List<CustomerDto>();
            var companies = await ResolveCompaniesAsync(userId, companyId);

            foreach (var company in companies)
            {
                var accessToken = await _quickBooksService.GetValidAccessTokenAsync(userId, company.Id);

                var request = new HttpRequestMessage(HttpMethod.Get,
                    $"{AppConstants.QuickBooksBaseUrl}/v3/company/{company.RealmId}/query?query=SELECT * FROM Customer WHERE Active = true MAXRESULTS 1000&minorversion=65");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    throw new ExternalServiceException($"Failed to fetch customers from QuickBooks company '{company.CompanyName}'.", error);
                }

                var json = await response.Content.ReadAsStringAsync();
                var data = JsonSerializer.Deserialize<JsonElement>(json);

                if (data.TryGetProperty("QueryResponse", out var queryResponse) &&
                    queryResponse.TryGetProperty("Customer", out var customerArray))
                {
                    foreach (var customer in customerArray.EnumerateArray())
                    {
                        customers.Add(new CustomerDto
                        {
                            Id = customer.GetProperty("Id").GetString()!,
                            CompanyId = company.Id,
                            CompanyName = company.CompanyName,
                            RealmId = company.RealmId,
                            DisplayName = customer.GetProperty("DisplayName").GetString()!,
                            Email = customer.TryGetProperty("PrimaryEmailAddr", out var emailObj) &&
                                    emailObj.TryGetProperty("Address", out var emailAddr)
                                ? emailAddr.GetString() : null,
                            Phone = customer.TryGetProperty("PrimaryPhone", out var phoneObj) &&
                                    phoneObj.TryGetProperty("FreeFormNumber", out var phoneNum)
                                ? phoneNum.GetString() : null,
                            Active = customer.GetProperty("Active").GetBoolean()
                        });
                    }
                }
            }

            return customers;
        }

        public async Task<CustomerDto> CreateCustomerAsync(string userId, CreateCustomerDto dto, string? companyId = null)
        {
            var company = await ResolveSingleCompanyAsync(userId, companyId);
            var accessToken = await _quickBooksService.GetValidAccessTokenAsync(userId, company.Id);
            var realmId = company.RealmId;

            var payload = new Dictionary<string, object>
            {
                ["DisplayName"] = dto.DisplayName
            };

            if (!string.IsNullOrEmpty(dto.FirstName))
                payload["GivenName"] = dto.FirstName;

            if (!string.IsNullOrEmpty(dto.LastName))
                payload["FamilyName"] = dto.LastName;

            if (!string.IsNullOrEmpty(dto.Email))
                payload["PrimaryEmailAddr"] = new { Address = dto.Email };

            if (!string.IsNullOrEmpty(dto.Phone))
                payload["PrimaryPhone"] = new { FreeFormNumber = dto.Phone };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{AppConstants.QuickBooksBaseUrl}/v3/company/{realmId}/customer?minorversion=65");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("Failed to create customer in QuickBooks.", error);
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            var customer = data.GetProperty("Customer");

            return new CustomerDto
            {
                Id = customer.GetProperty("Id").GetString()!,
                CompanyId = company.Id,
                CompanyName = company.CompanyName,
                RealmId = company.RealmId,
                DisplayName = customer.GetProperty("DisplayName").GetString()!,
                Email = customer.TryGetProperty("PrimaryEmailAddr", out var emailObj) &&
                        emailObj.TryGetProperty("Address", out var emailAddr)
                    ? emailAddr.GetString() : null,
                Phone = customer.TryGetProperty("PrimaryPhone", out var phoneObj) &&
                        phoneObj.TryGetProperty("FreeFormNumber", out var phoneNum)
                    ? phoneNum.GetString() : null,
                Active = customer.GetProperty("Active").GetBoolean()
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
