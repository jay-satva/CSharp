namespace MyProject.Application.DTOs.Account
{
    public class AccountDto
    {
        public string Id { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public string? AccountSubType { get; set; }
        public string? Description { get; set; }
        public bool Active { get; set; }
    }
}
