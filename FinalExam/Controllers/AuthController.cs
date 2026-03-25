using FinalExam.Data;
using FinalExam.Models;
using FinalExam.Services;
using Microsoft.AspNetCore.Mvc;

namespace FinalExam.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly QuickBooksService _qbService;
    private readonly UserRepository _userRepo;
    private readonly CompanyRepository _companyRepo;

    public AuthController(QuickBooksService qbService, UserRepository userRepo, CompanyRepository companyRepo)
    {
        _qbService = qbService;
        _userRepo = userRepo;
        _companyRepo = companyRepo;
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

        HttpContext.Session.SetString("authType", "manual");
        HttpContext.Session.SetString("userId", user.UserId);
        HttpContext.Session.SetString("email", user.Email);

        return Ok(new { message = "Logged in.", userId = user.UserId });
    }

    [HttpGet("sso/connect")]
    public IActionResult SsoConnect()
    {
        var state = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("oauth_state_sso", state);
        var url = _qbService.GetSsoAuthorizationUrl(state);
        return Redirect(url);
    }

    [HttpGet("/callback/sso")]
    public async Task<IActionResult> SsoCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error = null)
    {
        if (!string.IsNullOrEmpty(error))
            return Redirect($"http://localhost:5173/login?error={Uri.EscapeDataString(error)}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state))
            return Redirect("http://localhost:5173/login?error=missing_params");

        var savedState = HttpContext.Session.GetString("oauth_state_sso");
        if (state != savedState)
            return Redirect("http://localhost:5173/login?error=invalid_state");

        HttpContext.Session.Remove("oauth_state_sso");

        try
        {
            var (_, user) = await _qbService.ExchangeCodeForTokensAsync(
                code: code,
                realmId: null,
                userEmailFallbackForMissingIdToken: null,
                userNameFallbackForMissingIdToken: null,
                syncCompanyInfo: false);

            HttpContext.Session.SetString("userId", user.UserId);
            HttpContext.Session.SetString("email", user.Email);
            HttpContext.Session.SetString("authType", "intuit");

            return Redirect("http://localhost:5173/dashboard?status=success");
        }
        catch (Exception ex)
        {
            return Redirect($"http://localhost:5173/login?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpGet("qb/connect")]
    public IActionResult QbConnect()
    {
        var userId = HttpContext.Session.GetString("userId");
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        var state = Guid.NewGuid().ToString("N");
        HttpContext.Session.SetString("oauth_state_qb", state);

        var url = _qbService.GetQbAuthorizationUrl(state);
        return Redirect(url);
    }

    [HttpGet("/callback/qb")]
    public async Task<IActionResult> QbCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? realmId,
        [FromQuery] string? error = null)
    {
        if (!string.IsNullOrEmpty(error))
            return Redirect($"http://localhost:5173/dashboard?error={Uri.EscapeDataString(error)}");

        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(state) || string.IsNullOrEmpty(realmId))
            return Redirect("http://localhost:5173/dashboard?error=missing_params");

        var savedState = HttpContext.Session.GetString("oauth_state_qb");
        if (state != savedState)
            return Redirect("http://localhost:5173/dashboard?error=invalid_state");

        HttpContext.Session.Remove("oauth_state_qb");

        var fallbackEmail = HttpContext.Session.GetString("email");

        try
        {
            var (_, user) = await _qbService.ExchangeCodeForTokensAsync(
                code: code,
                realmId: realmId,
                userEmailFallbackForMissingIdToken: fallbackEmail,
                userNameFallbackForMissingIdToken: null,
                syncCompanyInfo: true);

            HttpContext.Session.SetString("userId", user.UserId);
            HttpContext.Session.SetString("email", user.Email);
            HttpContext.Session.SetString("authType", "intuit");
            return Redirect("http://localhost:5173/dashboard?status=connected");
        }
        catch (Exception ex)
        {
            return Redirect($"http://localhost:5173/dashboard?error={Uri.EscapeDataString(ex.Message)}");
        }
    }

    [HttpGet("user")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = HttpContext.Session.GetString("userId");
        if (string.IsNullOrEmpty(userId))
            return Unauthorized(new { message = "Not logged in." });

        var user = await _userRepo.GetByUserIdAsync(userId);
        return Ok(new
        {
            UserId = userId,
            Email = user?.Email,
            Name = user?.Name
        });
    }

    [HttpGet("/qb/companies")]
    public async Task<IActionResult> GetCompanies()
    {
        var userId = HttpContext.Session.GetString("userId");
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        var companies = await _companyRepo.GetByUserIdAsync(userId);
        return Ok(companies);
    }

    [HttpPost("/qb/companies/{realmId}/connect")]
    public async Task<IActionResult> ConnectCompany([FromRoute] string realmId)
    {
        var userId = HttpContext.Session.GetString("userId");
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        var updated = await _companyRepo.SetCompanyActiveAsync(userId, realmId, true);
        return updated ? Ok(new { message = "Company connected." }) : NotFound(new { message = "Company not found." });
    }

    [HttpPost("/qb/companies/{realmId}/disconnect")]
    public async Task<IActionResult> DisconnectCompany([FromRoute] string realmId)
    {
        var userId = HttpContext.Session.GetString("userId");
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized(new { message = "Not logged in." });

        var updated = await _companyRepo.SetCompanyActiveAsync(userId, realmId, false);
        return updated ? Ok(new { message = "Company disconnected." }) : NotFound(new { message = "Company not found." });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logged out." });
    }
}
