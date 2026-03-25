using Microsoft.AspNetCore.Mvc;
using OAuthDemo.Services;
using OAuthDemo.Data;
[ApiController]
[Route("[controller]")]
public class AuthController : Controller
{
    private readonly QuickBooksService _qbService;
    private readonly TokenRepository _tokenRepository;

    public AuthController(QuickBooksService qbService, TokenRepository tokenRepository)
    {
        _qbService = qbService;
        _tokenRepository = tokenRepository;
    }

    [HttpGet("connect")]
    public IActionResult Connect(string userId = "USER_123")
    {
        var state = Guid.NewGuid().ToString();
        HttpContext.Session.SetString("oauth_state", state);
        HttpContext.Session.SetString("pending_user_id", userId);

        var url = _qbService.GetAuthorizationUrl(state);

        return Redirect(url);
    }

    [HttpGet("/callback")]
    public async Task<IActionResult> Callback(string code, string state, string realmId)
    {
        var savedState = HttpContext.Session.GetString("oauth_state");
        var userId = HttpContext.Session.GetString("pending_user_id") ?? "USER_123";

        if (string.IsNullOrEmpty(state) || state != savedState)
        {
            return BadRequest("Invalid state. Potential CSRF attack detected.");
        }

        var tokens = await _qbService.ExchangeCodeForTokens(code, userId, realmId);

        return Redirect("/?success=true");
    }
}