using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs.Account;
using MyProject.Application.Interfaces;
using System.Security.Claims;

namespace MyProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ApiUser")]
    public class AccountController : ControllerBase
    {
        private readonly IAccountService _accountService;

        public AccountController(IAccountService accountService)
        {
            _accountService = accountService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAccounts([FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var accounts = await _accountService.GetAccountsAsync(userId, companyId);
            return Ok(accounts);
        }

        [HttpPost]
        public async Task<IActionResult> CreateAccount([FromBody] CreateAccountDto dto, [FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var account = await _accountService.CreateAccountAsync(userId, dto, companyId);
            return Ok(account);
        }
    }
}
