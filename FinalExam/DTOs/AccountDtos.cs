namespace FinalExam.DTOs;

public class AccountDto
{
    public string Id { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AcctNum { get; set; }
    public string AccountType { get; set; } = string.Empty;
    public bool Active { get; set; }
}

public class CreateAccountRequest
{
    public string RealmId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AcctNum { get; set; }
    public string AccountType { get; set; } = string.Empty;
}
