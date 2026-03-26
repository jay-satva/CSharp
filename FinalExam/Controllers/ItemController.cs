using System.Security.Claims;
using FinalExam.DTOs;
using FinalExam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalExam.Controllers;

[ApiController]
[Route("item")]
[Authorize]
public class ItemController : ControllerBase
{
    private readonly ItemService _itemService;

    public ItemController(ItemService itemService)
    {
        _itemService = itemService;
    }

    [HttpGet]
    public async Task<IActionResult> GetItems([FromQuery] string? realmId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        try
        {
            var items = await _itemService.GetItemsAsync(userId, realmId);
            return Ok(items);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateItem([FromBody] CreateItemRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        if (string.IsNullOrWhiteSpace(request.RealmId) || string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Company and item name are required." });

        if (request.Name.Contains(':') || request.Name.Contains('\t') || request.Name.Contains('\n') || request.Name.Contains('\r'))
            return BadRequest(new { message = "Item name cannot contain colons, tabs, or new lines." });

        if (string.IsNullOrWhiteSpace(request.IncomeAccountRef))
            return BadRequest(new { message = "Income account is required." });

        if (string.Equals(request.Type?.Trim(), "Inventory", StringComparison.OrdinalIgnoreCase) &&
            (string.IsNullOrWhiteSpace(request.ExpenseAccountRef) || string.IsNullOrWhiteSpace(request.AssetAccountRef)))
            return BadRequest(new { message = "Inventory items require expense and asset accounts." });

        try
        {
            var created = await _itemService.CreateItemAsync(userId, request);
            return Ok(created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{itemId}")]
    public async Task<IActionResult> UpdateItem([FromRoute] string itemId, [FromBody] UpdateItemRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        if (string.IsNullOrWhiteSpace(request.RealmId) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.SyncToken))
            return BadRequest(new { message = "Company, item name, and SyncToken are required." });

        if (request.Name.Contains(':') || request.Name.Contains('\t') || request.Name.Contains('\n') || request.Name.Contains('\r'))
            return BadRequest(new { message = "Item name cannot contain colons, tabs, or new lines." });

        try
        {
            var updated = await _itemService.UpdateItemAsync(userId, itemId, request);
            return Ok(updated);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{itemId}")]
    public async Task<IActionResult> DeleteItem(
        [FromRoute] string itemId,
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
            await _itemService.DeleteItemAsync(userId, realmId, itemId, syncToken);
            return Ok(new { message = "Item deleted successfully." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
