using MyProject.Application.DTOs.Item;

namespace MyProject.Application.Interfaces
{
    public interface IItemService
    {
        Task<List<ItemDto>> GetItemsAsync(string userId, string? companyId = null);
        Task<ItemDto> CreateItemAsync(string userId, CreateItemDto dto, string? companyId = null);
    }
}
