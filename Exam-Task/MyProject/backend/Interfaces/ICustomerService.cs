using MyProject.Application.DTOs.Customer;

namespace MyProject.Application.Interfaces
{
    public interface ICustomerService
    {
        Task<List<CustomerDto>> GetCustomersAsync(string userId, string? companyId = null);
        Task<CustomerDto> CreateCustomerAsync(string userId, CreateCustomerDto dto, string? companyId = null);
    }
}
