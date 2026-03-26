using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FinalExam.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FinalExam.Services;

public class JwtService
{
    private readonly JwtOptions _options;

    public JwtService(IOptions<JwtOptions> options)
    {
        _options = options.Value;
    }

    public string GenerateAccessToken(AppUser user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.UserId),
            new(ClaimTypes.Name, string.IsNullOrWhiteSpace(user.Name) ? "User" : user.Name)
        };

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        if (!string.IsNullOrWhiteSpace(user.IntuitSub))
            claims.Add(new Claim("intuit_sub", user.IntuitSub));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.ExpiryInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateOAuthStateToken(OAuthStatePayload payload)
    {
        var claims = new List<Claim>
        {
            new("mode", payload.Mode)
        };

        if (!string.IsNullOrWhiteSpace(payload.UserId))
            claims.Add(new Claim("user_id", payload.UserId));

        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.OAuthStateExpiryInMinutes),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public OAuthStatePayload ValidateOAuthStateToken(string token)
    {
        var principal = ValidateTokenInternal(token, validateLifetime: true);

        var mode = principal.FindFirst("mode")?.Value;
        if (string.IsNullOrWhiteSpace(mode))
            throw new SecurityTokenException("Invalid OAuth state.");

        return new OAuthStatePayload
        {
            Mode = mode,
            UserId = principal.FindFirst("user_id")?.Value
        };
    }

    private ClaimsPrincipal ValidateTokenInternal(string token, bool validateLifetime)
    {
        var handler = new JwtSecurityTokenHandler();
        return handler.ValidateToken(token, new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.Secret)),
            ValidateIssuer = true,
            ValidIssuer = _options.Issuer,
            ValidateAudience = true,
            ValidAudience = _options.Audience,
            ValidateLifetime = validateLifetime,
            ClockSkew = TimeSpan.Zero
        }, out _);
    }
}
