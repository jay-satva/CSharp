using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.Exceptions;
using MyProject.Application.Interfaces;
using System.Security.Claims;

namespace MyProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ApiUser")]
    public class QuickBooksController : ControllerBase
    {
        private readonly IQuickBooksService _quickBooksService;

        public QuickBooksController(IQuickBooksService quickBooksService)
        {
            _quickBooksService = quickBooksService;
        }

        [HttpGet("connect")]
        public IActionResult Connect()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var url = _quickBooksService.GetAuthorizationUrl(userId);
            return Ok(new { url });
        }

        [HttpGet("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback(
            [FromQuery] string code,
            [FromQuery] string realmId,
            [FromQuery] string state,
            [FromServices] IConfiguration configuration)
        {
            if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(realmId) || string.IsNullOrEmpty(state))
                throw new BadRequestException("QuickBooks callback is missing required parameters.");

            var userId = Uri.UnescapeDataString(state);
            var company = await _quickBooksService.HandleCallbackAsync(code, realmId, userId);
            var frontendUrl = configuration["Frontend:Url"]!;
            return Redirect($"{frontendUrl}/connection?connected=true");
        }

        [HttpPost("disconnect")]
        public async Task<IActionResult> Disconnect([FromQuery] string companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _quickBooksService.DisconnectAsync(userId, companyId);
            return Ok(new { message = "Disconnected from QuickBooks successfully." });
        }

        [HttpGet("company")]
        public async Task<IActionResult> GetConnectedCompany([FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var company = await _quickBooksService.GetConnectedCompanyAsync(userId, companyId);
            return Ok(company);
        }

        [HttpGet("companies")]
        public async Task<IActionResult> GetConnectedCompanies()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var companies = await _quickBooksService.GetConnectedCompaniesAsync(userId);
            return Ok(companies);
        }
    }
}




