using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoConnect.Models;
using MongoConnect.Services;

namespace MongoConnect.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly ItemServices _itemServices;
        public ItemController(ItemServices itemServices)
        {
            _itemServices = itemServices;
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var items = await _itemServices.GetAllAsync();
            return Ok(items);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var item = await _itemServices.GetByIdAsync(id);
            if (item == null) return NotFound("Item not found.");
            return Ok(item);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> Create(Items item)
        {
            var created = await _itemServices.CreateAsync(item);
            return Ok(created.Name);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, Items updated)
        {
            var existing = await _itemServices.GetByIdAsync(id);
            if (existing == null) return NotFound("Item not found.");
            var success = await _itemServices.UpdateAsync(id, updated);
            return success ? Ok("Item updated.") : StatusCode(500, "Update failed.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _itemServices.GetByIdAsync(id);
            if (existing == null) return NotFound("Item not found.");
            var success = await _itemServices.DeleteAsync(id);
            return success ? Ok("Item deleted.") : StatusCode(500, "Delete failed.");
        }
    }
}
