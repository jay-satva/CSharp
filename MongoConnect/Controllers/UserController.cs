using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MongoConnect.Models;
using MongoConnect.Services;
using System.Security.Claims;

namespace MongoConnect.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly UserServices _userServices;

        public UserController(UserServices userServices)
        {
            _userServices = userServices;
        }

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userServices.GetAllAsync();
            var requesterEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var result = users.Select(u => new
            {u.Id, u.Name, u.Email, u.Role, u.CreatedAt});
            return Ok(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var user = await _userServices.GetByIdAsync(id);
            if (user == null) return NotFound("User not found.");

            return Ok(new { user.Id, user.Name, user.Email, user.Role, user.CreatedAt });
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, Users updated)
        {
            var existing = await _userServices.GetByIdAsync(id);
            if (existing == null) return NotFound("User not found.");
            var emailid = User.FindFirst(ClaimTypes.Email)?.Value;
            if (existing.Email == emailid) return BadRequest("You cannot update your own account");

            var success = await _userServices.UpdateAsync(id, updated);
            return success ? Ok("User updated.") : StatusCode(500, "Update failed.");
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existing = await _userServices.GetByIdAsync(id);
            if (existing == null) return NotFound("User not found.");
            var emailid = User.FindFirst(ClaimTypes.Email)?.Value;
            if (existing.Email == emailid) return BadRequest("You cannot delete your own account");

            var success = await _userServices.DeleteAsync(id);
            return success ? Ok("User deleted.") : StatusCode(500, "Delete failed.");
        }
    }
}