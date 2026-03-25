using ADO_EFcore.Models;
using ADO_EFcore.Dto;

namespace ADO_EFcore.Repository
{
    public interface IItemRepository
    {
        Task<List<ItemDto>> GetAllAsync();
        Task<ItemDto?> GetByIdAsync(int id);
        Task<ItemDto> CreateAsync(Items item);
        Task<bool> UpdateAsync(Items item);
        Task<bool> DeleteAsync(int id);
    }
}