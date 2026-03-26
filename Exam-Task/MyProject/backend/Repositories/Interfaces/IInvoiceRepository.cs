using MyProject.Domain.Entities;

namespace MyProject.Infrastructure.Repositories.Interfaces
{
    public interface IInvoiceRepository
    {
        Task<List<Invoice>> GetAllByUserIdAsync(string userId);
        Task<Invoice?> GetByIdAsync(int id, string userId);
        Task<Invoice?> GetByQuickBooksIdAsync(string quickBooksInvoiceId, string userId);
        Task<Invoice> CreateAsync(Invoice invoice);
        Task UpdateAsync(Invoice invoice);
        Task DeleteAsync(int id, string userId);
    }
}