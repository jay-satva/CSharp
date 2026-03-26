namespace MyProject.Application.DTOs.Account
{
    public class CreateAccountDto
    {
        public string Name { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string? AccountSubType { get; set; }
        public string? Description { get; set; }
    }
}