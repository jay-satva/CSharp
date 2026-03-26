using System.Security.Claims;
using FinalExam.Data;
using FinalExam.DTOs;
using FinalExam.Models;
using FinalExam.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace FinalExam.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly QuickBooksService _qbService;
    private readonly UserRepository _userRepo;
    private readonly CompanyRepository _companyRepo;
    private readonly JwtService _jwtService;
    private readonly IConfiguration _configuration;

    public AuthController(
        QuickBooksService qbService,
        UserRepository userRepo,
        CompanyRepository companyRepo,
        JwtService jwtService,
        IConfiguration configuration)
    {
        _qbService = qbService;
        _userRepo = userRepo;
        _companyRepo = companyRepo;
        _jwtService = jwtService;
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Name) ||
            string.IsNullOrWhiteSpace(req.Email) ||
            string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Name, email, and password are required." });

        if (req.Password.Length < 6)
            return BadRequest(new { message = "Password must be at least 6 characters." });

        var existing = await _userRepo.GetByEmailAsync(req.Email);
        if (existing != null)
            return Conflict(new { message = "An account with that email already exists." });

        var user = new AppUser
        {
            Name = req.Name.Trim(),
            Email = req.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password)
        };

        await _userRepo.CreateAsync(user);
        return Ok(new { message = "Account created. Please sign in." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
            return BadRequest(new { message = "Email and password are required." });

        var user = await _userRepo.GetByEmailAsync(req.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid email or password." });

        if (string.IsNullOrWhiteSpace(user.PasswordHash))
            return Unauthorized(new { message = "This account does not have a password set." });

        var passwordValid = BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash);
        if (!passwordValid)
            return Unauthorized(new { message = "Invalid email or password." });

        return Ok(BuildAuthResponse(user));
    }

    [HttpGet("sso/connect")]
    public IActionResult SsoConnect([FromQuery] string mode = "signin")
    {
        var normalizedMode = string.Equals(mode, "signup", StringComparison.OrdinalIgnoreCase)
            ? "signup"
            : "signin";

        var state = _jwtService.GenerateOAuthStateToken(new OAuthStatePayload
        {
            Mode = normalizedMode
        });

        var url = _qbService.GetSsoAuthorizationUrl(state);
        return Redirect(url);
    }

    [HttpGet("/callback/sso")]
    public async Task<IActionResult> SsoCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error = null)
    {
        var frontendUrl = _configuration["Frontend:Url"]!;
        var authPath = "login";

        if (!string.IsNullOrWhiteSpace(state))
        {
            try
            {
                var payload = _jwtService.ValidateOAuthStateToken(state);
                authPath = string.Equals(payload.Mode, "signup", StringComparison.OrdinalIgnoreCase)
                    ? "signup"
                    : "login";
            }
            catch
            {
                authPath = "login";
            }
        }

        if (!string.IsNullOrEmpty(error))
            return Redirect($"{frontendUrl}/{authPath}?error={Uri.EscapeDataString(error)}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect($"{frontendUrl}/{authPath}?error=missing_params");

        OAuthStatePayload oauthState;
        try
        {
            oauthState = _jwtService.ValidateOAuthStateToken(state);
        }
        catch (SecurityTokenException)
        {
            return Redirect($"{frontendUrl}/{authPath}?error=invalid_state");
        }

        try
        {
            var (_, user) = await _qbService.ExchangeCodeForTokensAsync(
                code: code,
                realmId: null,
                targetUserId: null,
                userEmailFallbackForMissingIdToken: null,
                userNameFallbackForMissingIdToken: null,
                syncCompanyInfo: false);

            return Redirect(BuildFrontendAuthCallbackUrl(user, "success"));
        }
        catch (Exception ex)
        {
            return Redirect($"{frontendUrl}/{authPath}?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [Authorize]
    [HttpGet("qb/connect-url")]
    public IActionResult GetQuickBooksConnectUrl()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        var state = _jwtService.GenerateOAuthStateToken(new OAuthStatePayload
        {
            Mode = "qb",
            UserId = userId
        });

        var url = _qbService.GetQbAuthorizationUrl(state);
        return Ok(new { url });
    }

    [HttpGet("/callback/qb")]
    public async Task<IActionResult> QbCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? realmId,
        [FromQuery] string? error = null)
    {
        var frontendUrl = _configuration["Frontend:Url"]!;

        if (!string.IsNullOrEmpty(error))
            return Redirect($"{frontendUrl}/dashboard?error={Uri.EscapeDataString(error)}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state) || string.IsNullOrEmpty(realmId))
            return Redirect($"{frontendUrl}/dashboard?error=missing_params");

        OAuthStatePayload oauthState;
        try
        {
            oauthState = _jwtService.ValidateOAuthStateToken(state);
        }
        catch (SecurityTokenException)
        {
            return Redirect($"{frontendUrl}/dashboard?error=invalid_state");
        }

        if (string.IsNullOrWhiteSpace(oauthState.UserId))
            return Redirect($"{frontendUrl}/dashboard?error=missing_user_context");

        try
        {
            await _qbService.ExchangeCodeForTokensAsync(
                code: code,
                realmId: realmId,
                targetUserId: oauthState.UserId,
                userEmailFallbackForMissingIdToken: null,
                userNameFallbackForMissingIdToken: null,
                syncCompanyInfo: true);

            return Redirect($"{frontendUrl}/dashboard?status=connected");
        }
        catch (Exception ex)
        {
            return Redirect($"{frontendUrl}/dashboard?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [Authorize]
    [HttpGet("user")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        var user = await _userRepo.GetByUserIdAsync(userId);
        if (user == null)
            return Unauthorized(new { message = "User not found." });

        return Ok(new
        {
            UserId = user.UserId,
            Email = user.Email,
            Name = user.Name,
            IntuitSub = user.IntuitSub
        });
    }

    [Authorize]
    [HttpGet("/qb/companies")]
    public async Task<IActionResult> GetCompanies()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        var companies = await _companyRepo.GetByUserIdAsync(userId);
        return Ok(companies);
    }

    [Authorize]
    [HttpPost("/qb/companies/{realmId}/connect")]
    public async Task<IActionResult> ConnectCompany([FromRoute] string realmId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        var company = await _companyRepo.GetByUserIdAndRealmIdAsync(userId, realmId);
        if (company == null)
            return NotFound(new { message = "Company not found." });

        if (string.IsNullOrWhiteSpace(company.RefreshToken))
            return BadRequest(new { message = "This company was fully disconnected. Use Connect to QuickBooks to reconnect it." });

        var updated = await _companyRepo.SetCompanyActiveAsync(userId, realmId, true);
        return updated
            ? Ok(new { message = "Company connected." })
            : NotFound(new { message = "Company not found." });
    }

    [Authorize]
    [HttpPost("/qb/companies/{realmId}/disconnect")]
    public async Task<IActionResult> DisconnectCompany([FromRoute] string realmId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        try
        {
            var updated = await _qbService.DisconnectCompanyAsync(userId, realmId);
            return updated
                ? Ok(new { message = "Company disconnected." })
                : NotFound(new { message = "Company not found." });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        return Ok(new { message = "Logged out." });
    }

    private AuthResponseDto BuildAuthResponse(AppUser user)
    {
        return new AuthResponseDto
        {
            AccessToken = _jwtService.GenerateAccessToken(user),
            UserId = user.UserId,
            Name = string.IsNullOrWhiteSpace(user.Name) ? "User" : user.Name,
            Email = user.Email,
            IntuitSub = user.IntuitSub
        };
    }

    private string BuildFrontendAuthCallbackUrl(AppUser user, string status)
    {
        var frontendUrl = _configuration["Frontend:Url"]!;
        var auth = BuildAuthResponse(user);
        var query = new List<string>
        {
            $"accessToken={Uri.EscapeDataString(auth.AccessToken)}",
            $"userId={Uri.EscapeDataString(auth.UserId)}",
            $"name={Uri.EscapeDataString(auth.Name)}",
            $"status={Uri.EscapeDataString(status)}"
        };

        if (!string.IsNullOrWhiteSpace(auth.Email))
            query.Add($"email={Uri.EscapeDataString(auth.Email)}");

        if (!string.IsNullOrWhiteSpace(auth.IntuitSub))
            query.Add($"intuitSub={Uri.EscapeDataString(auth.IntuitSub)}");

        return $"{frontendUrl}/auth/callback?{string.Join("&", query)}";
    }
}
