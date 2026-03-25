using ADO_EFcore.Dto;
using ADO_EFcore.Models;
using ADO_EFcore.Services;
using Microsoft.AspNetCore.Mvc;

namespace ADO_EFcore.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;
        private readonly UserServices _userServices;

        public AuthController(JwtService jwtService, UserServices userServices)
        {
            _jwtService = jwtService;
            _userServices = userServices;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterReq request)
        {
            var existing = await _userServices.GetByEmailAsync(request.Email);
            if (existing != null)
                return Conflict(new { message = "A user with this email already exists" });

            if (request.Role != "Admin" && request.Role != "User")
                return BadRequest(new { message = "Role must be either 'Admin' or 'User'" });

            var createdUser = await _userServices.CreateAsync(request);

            return Ok(new
            {
                createdUser.Id,
                createdUser.Name,
                createdUser.Email,
                createdUser.Role,
                createdUser.CreatedAt
            });
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginReq request)
        {
            // Get full user only for password verification (internal use)
            var user = await _userServices.GetByEmailForLoginAsync(request.Email);

            if (user == null || !_userServices.VerifyPassword(request.Password, user.Password))
                return Unauthorized(new { message = "Invalid email or password." });

            var token = _jwtService.GenerateToken(user.Email, user.Role);

            return Ok(new { token });
        }
    }
}