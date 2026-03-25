using AccountingCRUD.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace AccountingCRUD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ItemController : ControllerBase
    {
        private readonly QuickBooksService _qbService;
        public ItemController(QuickBooksService qbService)
        {
            _qbService = qbService;
        }
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetItems(string userId)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Get, "query?query=select * from Item");
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpGet("{userId}/{itemId}")]
        public async Task<IActionResult> GetItem(string userId, string itemId)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Get, $"item/{itemId}");
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpPost("{userId}")]
        public async Task<IActionResult> CreateItem(string userId, [FromBody] object item)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Post, "item", item);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
