using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs.Item;
using MyProject.Application.Interfaces;
using System.Security.Claims;

namespace MyProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ApiUser")]
    public class ItemController : ControllerBase
    {
        private readonly IItemService _itemService;

        public ItemController(IItemService itemService)
        {
            _itemService = itemService;
        }

        [HttpGet]
        public async Task<IActionResult> GetItems([FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = await _itemService.GetItemsAsync(userId, companyId);
            return Ok(items);
        }

        [HttpPost]
        public async Task<IActionResult> CreateItem([FromBody] CreateItemDto dto, [FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var item = await _itemService.CreateItemAsync(userId, dto, companyId);
            return Ok(item);
        }
    }
}
