using System.Security.Claims;
using FinalExam.DTOs;
using FinalExam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinalExam.Controllers;

[ApiController]
[Route("account")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly AccountService _accountService;

    public AccountController(AccountService accountService)
    {
        _accountService = accountService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAccounts([FromQuery] string? realmId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        try
        {
            var accounts = await _accountService.GetAccountsAsync(userId, realmId);
            return Ok(accounts);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> CreateAccount([FromBody] CreateAccountRequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        if (string.IsNullOrWhiteSpace(request.RealmId) ||
            string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.AccountType))
            return BadRequest(new { message = "Company, account name, and account type are required." });

        if (request.Name.Contains('"') || request.Name.Contains(':'))
            return BadRequest(new { message = "Account name cannot contain double quotes or colon." });

        if (!string.IsNullOrWhiteSpace(request.AcctNum) && request.AcctNum.Contains(':'))
            return BadRequest(new { message = "Account number cannot contain colon." });

        if (request.Name.Trim().Length > 100)
            return BadRequest(new { message = "Account name cannot exceed 100 characters." });

        if (!string.IsNullOrWhiteSpace(request.AcctNum) && request.AcctNum.Trim().Length > 20)
            return BadRequest(new { message = "Account number cannot exceed 20 characters." });

        try
        {
            var created = await _accountService.CreateAccountAsync(userId, request);
            return Ok(created);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
