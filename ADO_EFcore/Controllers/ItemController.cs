using ADO_EFcore.ActionFilter;
using ADO_EFcore.Dto;
using ADO_EFcore.Models;
using ADO_EFcore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADO_EFcore.Controllers
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
        [ServiceFilter(typeof(LoggingActionFilter))]
        public async Task<IActionResult> GetAll()
        {
            var items = await _itemServices.GetAllAsync();
            return Ok(items);
        }

        [Authorize(Roles = "Admin,User")]
        [HttpGet("{id}")]
        [ServiceFilter(typeof(LoggingActionFilter))]
        public async Task<IActionResult> GetById(int id)
        {
            var item = await _itemServices.GetByIdAsync(id);
            if (item == null)
                return NotFound(new { message = "Item not found." });

            return Ok(item);
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ServiceFilter(typeof(LoggingActionFilter))]
        [ServiceFilter(typeof(ValidateModelStateFilter))]
        public async Task<IActionResult> Create(Items item)
        {
            var createdDto = await _itemServices.CreateAsync(item);
            return CreatedAtAction(nameof(GetById), new { id = createdDto.Id }, createdDto);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        [ServiceFilter(typeof(LoggingActionFilter))]
        [ServiceFilter(typeof(ValidateModelStateFilter))]
        public async Task<IActionResult> Update(int id, Items updated)
        {
            var success = await _itemServices.UpdateAsync(id, updated);
            return success
                ? Ok(new { message = "Item updated successfully." })
                : NotFound(new { message = "Item not found." });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [ServiceFilter(typeof(LoggingActionFilter))]
        public async Task<IActionResult> Delete(int id)
        {
            var success = await _itemServices.DeleteAsync(id);
            return success
                ? Ok(new { message = "Item deleted successfully." })
                : NotFound(new { message = "Item not found." });
        }
    }
}