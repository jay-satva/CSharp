using Microsoft.AspNetCore.Mvc;
using AccountingCRUD.Services;
using System.Text.Json;

namespace AccountingCRUD.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvoiceController : ControllerBase
    {
        private readonly QuickBooksService _qbService;

        public InvoiceController(QuickBooksService qbService)
        {
            _qbService = qbService;
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetInvoices(string userId)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Get, "query?query=select * from Invoice");
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{userId}/{invoiceId}")]
        public async Task<IActionResult> GetInvoice(string userId, string invoiceId)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Get, $"invoice/{invoiceId}");
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{userId}")]
        public async Task<IActionResult> CreateInvoice(string userId, [FromBody] object invoice)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Post, "invoice", invoice);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("{userId}/update")]
        public async Task<IActionResult> UpdateInvoice(string userId, [FromBody] object invoice)
        {
            try
            {
                var result = await _qbService.ExecuteRequestAsync(userId, HttpMethod.Post, "invoice?operation=update", invoice);
                return Ok(JsonSerializer.Deserialize<object>(result));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
