using System.Security.Claims;
using FinalExam.DTOs;
using FinalExam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalExam.Controllers;

[ApiController]
[Route("customer")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly CustomerService _customerService;

    public CustomerController(CustomerService customerService)
    {
        _customerService = customerService;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] string? realmId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        try
        {
            var customers = await _customerService.GetCustomersAsync(userId, realmId);
            return Ok(customers);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        if (string.IsNullOrWhiteSpace(request.RealmId))
            return BadRequest(new { message = "Company is required." });

        if (string.IsNullOrWhiteSpace(request.DisplayName) &&
            string.IsNullOrWhiteSpace(request.GivenName) &&
            string.IsNullOrWhiteSpace(request.FamilyName))
            return BadRequest(new { message = "Display name or at least one customer name field is required." });

        try
        {
            var created = await _customerService.CreateCustomerAsync(userId, request);
            return Ok(created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{customerId}")]
    public async Task<IActionResult> UpdateCustomer([FromRoute] string customerId, [FromBody] UpdateCustomerRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        if (string.IsNullOrWhiteSpace(request.RealmId) || string.IsNullOrWhiteSpace(request.SyncToken))
            return BadRequest(new { message = "Company and SyncToken are required." });

        if (string.IsNullOrWhiteSpace(request.DisplayName) &&
            string.IsNullOrWhiteSpace(request.GivenName) &&
            string.IsNullOrWhiteSpace(request.FamilyName))
            return BadRequest(new { message = "Display name or at least one customer name field is required." });

        try
        {
            var updated = await _customerService.UpdateCustomerAsync(userId, customerId, request);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{customerId}")]
    public async Task<IActionResult> DeleteCustomer(
        [FromRoute] string customerId,
        [FromQuery] string realmId,
        [FromQuery] string syncToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        if (string.IsNullOrWhiteSpace(realmId) || string.IsNullOrWhiteSpace(syncToken))
            return BadRequest(new { message = "Company and SyncToken are required." });

        try
        {
            await _customerService.DeleteCustomerAsync(userId, realmId, customerId, syncToken);
            return Ok(new { message = "Customer deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
