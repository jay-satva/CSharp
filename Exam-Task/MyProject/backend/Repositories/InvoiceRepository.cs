using Microsoft.EntityFrameworkCore;
using MyProject.Domain.Entities;
using MyProject.Infrastructure.Data;
using MyProject.Infrastructure.Repositories.Interfaces;

namespace MyProject.Infrastructure.Repositories
{
    public class InvoiceRepository : IInvoiceRepository
    {
        private readonly AppDbContext _context;

        public InvoiceRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<Invoice>> GetAllByUserIdAsync(string userId)
        {
            return await _context.Invoices
                .Include(i => i.LineItems)
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
        }

        public async Task<Invoice?> GetByIdAsync(int id, string userId)
        {
            return await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == userId);
        }

        public async Task<Invoice?> GetByQuickBooksIdAsync(string quickBooksInvoiceId, string userId)
        {
            return await _context.Invoices
                .Include(i => i.LineItems)
                .FirstOrDefaultAsync(i => i.QuickBooksInvoiceId == quickBooksInvoiceId && i.UserId == userId);
        }

        public async Task<Invoice> CreateAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
            return invoice;
        }

        public async Task UpdateAsync(Invoice invoice)
        {
            invoice.UpdatedAt = DateTime.UtcNow;
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id, string userId)
        {
            var invoice = await GetByIdAsync(id, userId);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }
    }
}