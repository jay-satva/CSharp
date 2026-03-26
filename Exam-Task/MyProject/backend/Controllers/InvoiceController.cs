using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs.Invoice;
using MyProject.Application.Interfaces;
using System.Security.Claims;

namespace MyProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ApiUser")]
    public class InvoiceController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoiceController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetInvoices([FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var invoices = await _invoiceService.GetInvoicesAsync(userId, companyId);
            return Ok(invoices);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(int id, [FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id, userId, companyId);
            return Ok(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceDto dto, [FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var invoice = await _invoiceService.CreateInvoiceAsync(userId, dto, companyId);
            return Ok(invoice);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoice(int id, [FromBody] UpdateInvoiceDto dto, [FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var invoice = await _invoiceService.UpdateInvoiceAsync(id, userId, dto, companyId);
            return Ok(invoice);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInvoice(int id, [FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _invoiceService.DeleteInvoiceAsync(id, userId, companyId);
            return Ok(new { message = "Invoice deleted successfully." });
        }
    }
}
