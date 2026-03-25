using Microsoft.AspNetCore.Mvc;
using AccountingCRUD.Repository;

namespace AccountingCRUD.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FetchAccessToken : ControllerBase
    {
        private readonly TokenRepository _tokenRepository;

        public FetchAccessToken(TokenRepository tokenRepository)
        {
            _tokenRepository = tokenRepository;
        }

        // GET: api/FetchAccessToken/{userId}
        [HttpGet("{userId}")]
        public async Task<IActionResult> Get(string userId)
        {
            try
            {
                var token = await _tokenRepository.GetTokenByUserIdAsync(userId);
                if (token == null)
                {
                    return NotFound(new { message = $"No token found for user: {userId}" });
                }

                var isExpired = DateTime.UtcNow >= token.AccessTokenExpiry;

                return Ok(new
                {
                    UserId = token.UserId,
                    RealmId = token.RealmId,
                    AccessToken = token.AccessToken,
                    ExpiresAt = token.AccessTokenExpiry,
                    IsExpired = isExpired,
                    Status = isExpired ? "Expired" : "Valid"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
