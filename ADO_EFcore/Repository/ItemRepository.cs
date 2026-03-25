using ADO_EFcore.Dto;
using ADO_EFcore.Models;
using Microsoft.EntityFrameworkCore;

namespace ADO_EFcore.Repository
{
    public class ItemRepository : IItemRepository
    {
        private readonly AppDbContext _context;
        public ItemRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<ItemDto>> GetAllAsync()
        {
            return await _context.Items
                .Select(i => new ItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Price = i.Price
                })
                .ToListAsync();
        }

        public async Task<ItemDto?> GetByIdAsync(int id)
        {
            return await _context.Items
                .Where(i => i.Id == id)
                .Select(i => new ItemDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Price = i.Price
                })
                .FirstOrDefaultAsync();
        }

        public async Task<ItemDto> CreateAsync(Items item)
        {
            await _context.Items.AddAsync(item);
            await _context.SaveChangesAsync();
            return new ItemDto
            {
                Id = item.Id,
                Name = item.Name,
                Price = item.Price
            };
        }

        public async Task<bool> UpdateAsync(Items item)
        {
            _context.Items.Update(item);
            return await _context.SaveChangesAsync() > 0;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var item = await _context.Items.FindAsync(id);
            if (item == null) return false;
            _context.Items.Remove(item);
            return await _context.SaveChangesAsync() > 0;
        }
    }
}