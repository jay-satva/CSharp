using System.Security.Claims;
using FinalExam.DTOs;
using FinalExam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalExam.Controllers;

[ApiController]
[Route("invoice")]
[Authorize]
public class InvoiceController : ControllerBase
{
    private readonly InvoiceService _invoiceService;

    public InvoiceController(InvoiceService invoiceService)
    {
        _invoiceService = invoiceService;
    }

    [HttpGet]
    public async Task<IActionResult> GetInvoices()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        try
        {
            var invoices = await _invoiceService.GetInvoicesAsync(userId);
            return Ok(invoices);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetInvoice([FromRoute] int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        try
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(userId, id);
            return Ok(invoice);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateInvoice([FromBody] CreateInvoiceRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        if (string.IsNullOrWhiteSpace(request.RealmId))
            return BadRequest(new { message = "Please choose a connected company first." });

        if (string.IsNullOrWhiteSpace(request.CustomerRef) || string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(new { message = "Customer is required." });

        if (request.LineItems == null || request.LineItems.Count == 0)
            return BadRequest(new { message = "At least one line item is required." });

        if (request.LineItems.Any(lineItem =>
                string.IsNullOrWhiteSpace(lineItem.ItemRef) ||
                string.IsNullOrWhiteSpace(lineItem.ItemName) ||
                lineItem.Quantity <= 0 ||
                lineItem.UnitPrice < 0))
        {
            return BadRequest(new { message = "Each line item must have an item, quantity greater than 0, and a valid unit price." });
        }

        if (request.DueDate.HasValue && request.DueDate.Value.Date < request.InvoiceDate.Date)
            return BadRequest(new { message = "Due date must be on or after the invoice date." });

        try
        {
            var created = await _invoiceService.CreateInvoiceAsync(userId, request);
            return Ok(created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateInvoice([FromRoute] int id, [FromBody] UpdateInvoiceRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        if (string.IsNullOrWhiteSpace(request.CustomerRef) || string.IsNullOrWhiteSpace(request.CustomerName))
            return BadRequest(new { message = "Customer is required." });

        if (request.LineItems == null || request.LineItems.Count == 0)
            return BadRequest(new { message = "At least one line item is required." });

        if (request.LineItems.Any(lineItem =>
                string.IsNullOrWhiteSpace(lineItem.ItemRef) ||
                string.IsNullOrWhiteSpace(lineItem.ItemName) ||
                lineItem.Quantity <= 0 ||
                lineItem.UnitPrice < 0))
        {
            return BadRequest(new { message = "Each line item must have an item, quantity greater than 0, and a valid unit price." });
        }

        if (request.DueDate.HasValue && request.DueDate.Value.Date < request.InvoiceDate.Date)
            return BadRequest(new { message = "Due date must be on or after the invoice date." });

        try
        {
            var updated = await _invoiceService.UpdateInvoiceAsync(userId, id, request);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteInvoice([FromRoute] int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        try
        {
            await _invoiceService.DeleteInvoiceAsync(userId, id);
            return Ok(new { message = "Invoice deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
