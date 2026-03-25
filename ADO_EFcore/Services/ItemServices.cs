using ADO_EFcore.Dto;
using ADO_EFcore.Models;
using ADO_EFcore.Repository;

namespace ADO_EFcore.Services
{
    public class ItemServices
    {
        private readonly IItemRepository _itemRepository;

        public ItemServices(IItemRepository itemRepository)
        {
            _itemRepository = itemRepository;
        }

        public async Task<List<ItemDto>> GetAllAsync() => await _itemRepository.GetAllAsync();

        public async Task<ItemDto?> GetByIdAsync(int id) => await _itemRepository.GetByIdAsync(id);

        public async Task<ItemDto> CreateAsync(Items item) => await _itemRepository.CreateAsync(item);

        public async Task<bool> UpdateAsync(int id, Items updated)
        {
            var itemToUpdate = new Items
            {
                Id = id,
                Name = updated.Name,
                Description = updated.Description,
                Price = updated.Price
            };

            return await _itemRepository.UpdateAsync(itemToUpdate);
        }

        public async Task<bool> DeleteAsync(int id) => await _itemRepository.DeleteAsync(id);
    }
}