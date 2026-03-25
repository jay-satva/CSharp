using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly JwtService _jwtService;

        public AuthController(JwtService jwtService)
        {
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public IActionResult Login(LoginRequest request)
        {
            if (request.Email == "jay@gmail.com" &&
                request.Password == "jay123")
            {
                var token = _jwtService.GenerateToken(request.Email, "Admin");
                return Ok(new { token });
            }
            return Unauthorized("Invalid credentials");
        }
    }
}