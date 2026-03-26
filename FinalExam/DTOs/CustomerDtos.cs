namespace FinalExam.DTOs;

public class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string SyncToken { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool Active { get; set; }
}

public class CreateCustomerRequest
{
    public string RealmId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

public class UpdateCustomerRequest : CreateCustomerRequest
{
    public string SyncToken { get; set; } = string.Empty;
}
