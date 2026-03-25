using ADO_EFcore.ActionFilter;
using ADO_EFcore.Dto;
using ADO_EFcore.Models;
using ADO_EFcore.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ADO_EFcore.Controllers
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
        [ServiceFilter(typeof(LoggingActionFilter))]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userServices.GetAllAsync();
            var requesterEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            return Ok(users);
        }

        [Authorize(Roles = "Admin")]
        [HttpGet("{id}")]
        [ServiceFilter(typeof(LoggingActionFilter))]
        public async Task<IActionResult> GetById(int id)   
        {
            var user = await _userServices.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });
            return Ok(user);
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        [ServiceFilter(typeof(LoggingActionFilter))]
        [ServiceFilter(typeof(ValidateModelStateFilter))]
        public async Task<IActionResult> Update(int id, Users updated)
        {
            var existing = await _userServices.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "User not found." });

            var requesterEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (existing.Email == requesterEmail) return BadRequest(new { message = "You cannot update your own account." });

            var success = await _userServices.UpdateAsync(id, updated);
            return success
                ? Ok(new { message = "User updated successfully." })
                : StatusCode(500, new { message = "Update failed." });
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete("{id}")]
        [ServiceFilter(typeof(LoggingActionFilter))]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _userServices.GetByIdAsync(id);
            if (existing == null) return NotFound(new { message = "User not found." });

            var requesterEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (existing.Email == requesterEmail) return BadRequest(new { message = "You cannot delete your own account." });

            var success = await _userServices.DeleteAsync(id);
            return success
                ? Ok(new { message = "User deleted successfully." })
                : StatusCode(500, new { message = "Delete failed." });
        }
    }
}