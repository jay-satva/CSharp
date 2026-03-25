using Microsoft.AspNetCore.Mvc;
using AccountingCRUD.Services;
using System.Text.Json;

namespace AccountingCRUD.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BillController : ControllerBase
    {
        private readonly QuickBooksService _qbService;

        public BillController(QuickBooksService qbService)
        {
            _qbService = qbService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetBills(string userId)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Get, "query?query=select * from Bill");
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{userId}/{billId}")]
        public async Task<IActionResult> GetBill(string userId, string billId)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Get, $"bill/{billId}");
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> CreateBill(string userId, [FromBody] object bill)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Post, "bill", bill);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{userId}/update")]
        public async Task<IActionResult> UpdateBill(string userId, [FromBody] object bill)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Post, "bill?operation=update", bill);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
