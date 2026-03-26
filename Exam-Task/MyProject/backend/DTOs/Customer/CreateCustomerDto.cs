namespace MyProject.Application.DTOs.Customer
{
    public class CreateCustomerDto
    {
        public string DisplayName { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }
}