using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs.Auth;
using MyProject.Application.Exceptions;
using MyProject.Application.Interfaces;
using System.Security.Claims;

namespace MyProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("signup")]
        public async Task<IActionResult> SignUp([FromBody] SignUpDto dto)
        {
            var result = await _authService.SignUpAsync(dto);
            return Ok(result);
        }

        [HttpPost("signin")]
        public async Task<IActionResult> SignIn([FromBody] SignInDto dto)
        {
            var result = await _authService.SignInAsync(dto);
            return Ok(result);
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto dto)
        {
            var result = await _authService.RefreshTokenAsync(dto.RefreshToken);
            return Ok(result);
        }

        [Authorize(Policy = "ApiUser")]
        [HttpPost("revoke-token")]
        public async Task<IActionResult> RevokeToken([FromBody] RefreshTokenRequestDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _authService.RevokeTokenAsync(dto.RefreshToken, userId);
            return Ok(new { message = "Token revoked successfully." });
        }

        [HttpGet("intuit/login")]
        public IActionResult IntuitLogin([FromQuery] string mode = "signin")
        {
            var clientId = _configuration["QuickBooks:ClientId"]!;
            var redirectUri = Uri.EscapeDataString(_configuration["QuickBooks:SsoRedirectUri"]!);
            var scope = Uri.EscapeDataString("openid profile email");
            var normalizedMode = mode.Equals("signup", StringComparison.OrdinalIgnoreCase) ? "signup" : "signin";
            var state = Uri.EscapeDataString(normalizedMode);
            var url = $"https://appcenter.intuit.com/connect/oauth2?client_id={clientId}&redirect_uri={redirectUri}&response_type=code&scope={scope}&state={state}";
            return Ok(new { url });
        }


        [HttpGet("intuit/callback")]
        public async Task<IActionResult> IntuitCallback(
            [FromQuery] string? code,
            [FromQuery] string? state,
            [FromQuery] string? error,
            [FromQuery] string? error_description)
        {
            var frontendUrl = _configuration["Frontend:Url"]!;
            var authPath = string.Equals(state, "signup", StringComparison.OrdinalIgnoreCase) ? "signup" : "signin";

            if (!string.IsNullOrWhiteSpace(error))
            {
                var message = string.IsNullOrWhiteSpace(error_description) ? "Intuit sign-in was canceled or denied." : error_description;
                return Redirect($"{frontendUrl}/{authPath}?authError={Uri.EscapeDataString(message)}");
            }

            if (string.IsNullOrWhiteSpace(code))
                return Redirect($"{frontendUrl}/{authPath}?authError={Uri.EscapeDataString("Authorization code is missing from Intuit callback.")}");

            try
            {
                var result = await _authService.IntuitCallbackAsync(code, state ?? string.Empty);
                var incompleteFields = string.Join(",", result.IncompleteFields);
                return Redirect(
                    $"{frontendUrl}/auth/callback?accessToken={result.AccessToken}" +
                    $"&refreshToken={result.RefreshToken}" +
                    $"&userId={result.UserId}" +
                    $"&firstName={Uri.EscapeDataString(result.FirstName)}" +
                    $"&lastName={Uri.EscapeDataString(result.LastName)}" +
                    $"&email={Uri.EscapeDataString(result.Email)}" +
                    $"&authProvider={Uri.EscapeDataString(result.AuthProvider)}" +
                    $"&phoneNumber={Uri.EscapeDataString(result.PhoneNumber ?? string.Empty)}" +
                    $"&profilePhotoUrl={Uri.EscapeDataString(result.ProfilePhotoUrl ?? string.Empty)}" +
                    $"&incompleteFields={Uri.EscapeDataString(incompleteFields)}");
            }
            catch (AppException ex)
            {
                return Redirect($"{frontendUrl}/{authPath}?authError={Uri.EscapeDataString(ex.Message)}");
            }
            catch (Exception)
            {
                return Redirect($"{frontendUrl}/{authPath}?authError={Uri.EscapeDataString("We couldn't sign you in with Intuit right now. Please try again.")}");
            }
        }
    }
}
