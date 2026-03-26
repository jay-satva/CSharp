using MyProject.Application.DTOs.Invoice;

namespace MyProject.Application.Interfaces
{
    public interface IInvoiceService
    {
        Task<List<InvoiceDto>> GetInvoicesAsync(string userId, string? companyId = null);
        Task<InvoiceDto> GetInvoiceByIdAsync(int id, string userId, string? companyId = null);
        Task<InvoiceDto> CreateInvoiceAsync(string userId, CreateInvoiceDto dto, string? companyId = null);
        Task<InvoiceDto> UpdateInvoiceAsync(int id, string userId, UpdateInvoiceDto dto, string? companyId = null);
        Task DeleteInvoiceAsync(int id, string userId, string? companyId = null);
    }
}
