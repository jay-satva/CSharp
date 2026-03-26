using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyProject.Application.DTOs.Customer;
using MyProject.Application.Interfaces;
using System.Security.Claims;

namespace MyProject.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "ApiUser")]
    public class CustomerController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomerController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        [HttpGet]
        public async Task<IActionResult> GetCustomers([FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var customers = await _customerService.GetCustomersAsync(userId, companyId);
            return Ok(customers);
        }

        [HttpPost]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto, [FromHeader(Name = "X-Company-Id")] string? companyId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var customer = await _customerService.CreateCustomerAsync(userId, dto, companyId);
            return Ok(customer);
        }
    }
}
