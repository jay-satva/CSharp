using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AutoMapper;
using MyProject.Application.DTOs.Invoice;
using MyProject.Application.Exceptions;
using MyProject.Application.Interfaces;
using MyProject.Domain.Constants;
using MyProject.Domain.Entities;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Application.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly IInvoiceRepository _invoiceRepository;
        private readonly ICompanyRepository _companyRepository;
        private readonly IQuickBooksService _quickBooksService;
        private readonly IMapper _mapper;
        private readonly HttpClient _httpClient;

        public InvoiceService(
            IInvoiceRepository invoiceRepository,
            ICompanyRepository companyRepository,
            IQuickBooksService quickBooksService,
            IMapper mapper,
            IHttpClientFactory httpClientFactory)
        {
            _invoiceRepository = invoiceRepository;
            _companyRepository = companyRepository;
            _quickBooksService = quickBooksService;
            _mapper = mapper;
            _httpClient = httpClientFactory.CreateClient();
        }

        public async Task<List<InvoiceDto>> GetInvoicesAsync(string userId, string? companyId = null)
        {
            var invoices = await _invoiceRepository.GetAllByUserIdAsync(userId);
            var companies = await ResolveConnectedCompaniesAsync(userId, companyId);
            var companyByRealm = companies.ToDictionary(c => c.RealmId, c => c);
            invoices = invoices.Where(i => companyByRealm.ContainsKey(i.RealmId)).ToList();

            var dtos = _mapper.Map<List<InvoiceDto>>(invoices);
            foreach (var dto in dtos)
            {
                if (companyByRealm.TryGetValue(dto.RealmId, out var company))
                {
                    dto.CompanyId = company.Id;
                    dto.CompanyName = company.CompanyName;
                    dto.RealmId = company.RealmId;
                }
            }

            return dtos;
        }

        public async Task<InvoiceDto> GetInvoiceByIdAsync(int id, string userId, string? companyId = null)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id, userId);
            if (invoice == null)
                throw new NotFoundException("Invoice not found.");

            var company = await ResolveConnectedCompanyByRealmAsync(userId, invoice.RealmId);
            if (company == null || (!string.IsNullOrWhiteSpace(companyId) && !string.Equals(company.Id, companyId, StringComparison.Ordinal)))
                throw new NotFoundException("Invoice not found.");

            var dto = _mapper.Map<InvoiceDto>(invoice);
            dto.CompanyId = company.Id;
            dto.CompanyName = company.CompanyName;
            dto.RealmId = company.RealmId;
            return dto;
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(string userId, CreateInvoiceDto dto, string? companyId = null)
        {
            var targetCompanyId = !string.IsNullOrWhiteSpace(dto.CompanyId) ? dto.CompanyId : companyId;
            if (string.IsNullOrWhiteSpace(targetCompanyId))
                throw new BadRequestException("Please select a connected company before creating an invoice.");

            var company = await ResolveConnectedCompanyAsync(userId, targetCompanyId);
            var accessToken = await _quickBooksService.GetValidAccessTokenAsync(userId, company.Id);
            var realmId = company.RealmId;

            var lineItems = dto.LineItems.Select(li => new
            {
                Amount = li.Quantity * li.UnitPrice,
                Description = li.Description,
                DetailType = "SalesItemLineDetail",
                SalesItemLineDetail = new
                {
                    ItemRef = new { value = li.ItemRef, name = li.ItemName },
                    Qty = li.Quantity,
                    UnitPrice = li.UnitPrice
                }
            }).ToList();

            var payload = new
            {
                CustomerRef = new { value = dto.CustomerRef, name = dto.CustomerName },
                TxnDate = dto.InvoiceDate.ToString("yyyy-MM-dd"),
                DueDate = dto.DueDate.ToString("yyyy-MM-dd"),
                PrivateNote = dto.Memo,
                Line = lineItems
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{AppConstants.QuickBooksBaseUrl}/v3/company/{realmId}/invoice?minorversion=65");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("Failed to create invoice in QuickBooks.", error);
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            var qbInvoice = data.GetProperty("Invoice");

            var totalAmount = dto.LineItems.Sum(li => li.Quantity * li.UnitPrice);

            var invoice = new Invoice
            {
                UserId = userId,
                RealmId = realmId,
                QuickBooksInvoiceId = qbInvoice.GetProperty("Id").GetString()!,
                CustomerRef = dto.CustomerRef,
                CustomerName = dto.CustomerName,
                InvoiceDate = dto.InvoiceDate,
                DueDate = dto.DueDate,
                Memo = dto.Memo,
                TotalAmount = totalAmount,
                Status = "Draft",
                LineItems = dto.LineItems.Select(li => new InvoiceLineItem
                {
                    ItemRef = li.ItemRef,
                    ItemName = li.ItemName,
                    Description = li.Description,
                    Quantity = li.Quantity,
                    UnitPrice = li.UnitPrice,
                    Amount = li.Quantity * li.UnitPrice
                }).ToList()
            };

            var created = await _invoiceRepository.CreateAsync(invoice);
            var createdDto = _mapper.Map<InvoiceDto>(created);
            createdDto.CompanyId = company.Id;
            createdDto.CompanyName = company.CompanyName;
            createdDto.RealmId = company.RealmId;
            return createdDto;
        }

        public async Task<InvoiceDto> UpdateInvoiceAsync(int id, string userId, UpdateInvoiceDto dto, string? companyId = null)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id, userId);
            if (invoice == null)
                throw new NotFoundException("Invoice not found.");

            var company = await ResolveConnectedCompanyByRealmAsync(userId, invoice.RealmId);
            if (company == null)
                throw new NotFoundException("Invoice not found.");
            if (!string.IsNullOrWhiteSpace(companyId) && !string.Equals(company.Id, companyId, StringComparison.Ordinal))
                throw new NotFoundException("Invoice not found.");

            var accessToken = await _quickBooksService.GetValidAccessTokenAsync(userId, company.Id);
            var realmId = company.RealmId;

            var qbInvoice = await GetQuickBooksInvoiceAsync(accessToken, realmId, invoice.QuickBooksInvoiceId);
            var syncToken = qbInvoice.GetProperty("SyncToken").GetString()!;

            var lineItems = dto.LineItems.Select(li => new
            {
                Amount = li.Quantity * li.UnitPrice,
                Description = li.Description,
                DetailType = "SalesItemLineDetail",
                SalesItemLineDetail = new
                {
                    ItemRef = new { value = li.ItemRef, name = li.ItemName },
                    Qty = li.Quantity,
                    UnitPrice = li.UnitPrice
                }
            }).ToList();

            var payload = new
            {
                Id = invoice.QuickBooksInvoiceId,
                SyncToken = syncToken,
                CustomerRef = new { value = dto.CustomerRef, name = dto.CustomerName },
                TxnDate = dto.InvoiceDate.ToString("yyyy-MM-dd"),
                DueDate = dto.DueDate.ToString("yyyy-MM-dd"),
                PrivateNote = dto.Memo,
                Line = lineItems
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{AppConstants.QuickBooksBaseUrl}/v3/company/{realmId}/invoice?minorversion=65");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("Failed to update invoice in QuickBooks.", error);
            }

            invoice.CustomerRef = dto.CustomerRef;
            invoice.CustomerName = dto.CustomerName;
            invoice.InvoiceDate = dto.InvoiceDate;
            invoice.DueDate = dto.DueDate;
            invoice.Memo = dto.Memo;
            invoice.TotalAmount = dto.LineItems.Sum(li => li.Quantity * li.UnitPrice);
            invoice.LineItems = dto.LineItems.Select(li => new InvoiceLineItem
            {
                ItemRef = li.ItemRef,
                ItemName = li.ItemName,
                Description = li.Description,
                Quantity = li.Quantity,
                UnitPrice = li.UnitPrice,
                Amount = li.Quantity * li.UnitPrice
            }).ToList();

            await _invoiceRepository.UpdateAsync(invoice);
            var updatedDto = _mapper.Map<InvoiceDto>(invoice);
            updatedDto.CompanyId = company.Id;
            updatedDto.CompanyName = company.CompanyName;
            updatedDto.RealmId = company.RealmId;
            return updatedDto;
        }

        public async Task DeleteInvoiceAsync(int id, string userId, string? companyId = null)
        {
            var invoice = await _invoiceRepository.GetByIdAsync(id, userId);
            if (invoice == null)
                throw new NotFoundException("Invoice not found.");

            var company = await ResolveConnectedCompanyByRealmAsync(userId, invoice.RealmId);
            if (company == null)
                throw new NotFoundException("Invoice not found.");
            if (!string.IsNullOrWhiteSpace(companyId) && !string.Equals(company.Id, companyId, StringComparison.Ordinal))
                throw new NotFoundException("Invoice not found.");

            var accessToken = await _quickBooksService.GetValidAccessTokenAsync(userId, company.Id);
            var realmId = company.RealmId;

            var qbInvoice = await GetQuickBooksInvoiceAsync(accessToken, realmId, invoice.QuickBooksInvoiceId);
            var syncToken = qbInvoice.GetProperty("SyncToken").GetString()!;

            var payload = new
            {
                Id = invoice.QuickBooksInvoiceId,
                SyncToken = syncToken
            };

            var request = new HttpRequestMessage(HttpMethod.Post,
                $"{AppConstants.QuickBooksBaseUrl}/v3/company/{realmId}/invoice?operation=delete&minorversion=65");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("Failed to delete invoice in QuickBooks.", error);
            }

            await _invoiceRepository.DeleteAsync(id, userId);
        }

        private async Task<JsonElement> GetQuickBooksInvoiceAsync(string accessToken, string realmId, string invoiceId)
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"{AppConstants.QuickBooksBaseUrl}/v3/company/{realmId}/invoice/{invoiceId}?minorversion=65");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new ExternalServiceException("Failed to fetch invoice from QuickBooks.", error);
            }

            var json = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<JsonElement>(json);
            return data.GetProperty("Invoice");
        }

        private async Task<List<Company>> ResolveConnectedCompaniesAsync(string userId, string? companyId)
        {
            if (!string.IsNullOrWhiteSpace(companyId))
            {
                var company = await ResolveConnectedCompanyAsync(userId, companyId);
                return new List<Company> { company };
            }

            var connected = await _companyRepository.GetConnectedByUserIdAsync(userId);
            return connected;
        }

        private async Task<Company> ResolveConnectedCompanyAsync(string userId, string companyId)
        {
            var selected = await _companyRepository.GetByIdAsync(companyId);
            if (selected == null || selected.UserId != userId || !selected.IsConnected)
                throw new BadRequestException("Selected QuickBooks company is not connected.");

            return selected;
        }

        private async Task<Company?> ResolveConnectedCompanyByRealmAsync(string userId, string realmId)
        {
            var company = await _companyRepository.GetByUserAndRealmIdAsync(userId, realmId);
            if (company == null || !company.IsConnected)
                return null;

            return company;
        }
    }
}
