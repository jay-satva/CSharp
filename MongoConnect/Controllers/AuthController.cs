using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using MongoConnect.Models;
using MongoConnect.Services;

namespace MongoConnect.Controllers
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
            if (existing != null) return Conflict("A user with this email already exists");

            if (request.Role != "Admin" && request.Role != "User") return BadRequest("Role must be either 'Admin' or 'User'");

            var user = await _userServices.CreateAsync(request);
            return Ok(new {user.Id, user.Name, user.Email, user.Role, user.CreatedAt});
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginReq request)
        {
            var user = await _userServices.GetByEmailAsync(request.Email);

            if (user == null || !_userServices.VerifyPassword(request.Password, user.Password))
                return Unauthorized("Invalid email or password.");

            var token = _jwtService.GenerateToken(user.Email, user.Role);
            return Ok(new { token });
        }
    }
}