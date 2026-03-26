using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs.User;
using MyProject.Application.Interfaces;
using System.Security.Claims;

namespace MyProject.API.Controllers
{
    [ApiController]
    [Authorize(Policy = "ApiUser")]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var profile = await _userService.GetProfileAsync(userId);
            return Ok(profile);
        }

        [HttpPatch("me")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserProfileDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var updated = await _userService.UpdateProfileAsync(userId, dto);
            return Ok(updated);
        }
    }
}
