namespace FinalExam.DTOs;

public class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string SyncToken { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
    public string ConnectedCompanyName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? GivenName { get; set; }
    public string? MiddleName { get; set; }
    public string? FamilyName { get; set; }
    public string? Title { get; set; }
    public string? Suffix { get; set; }
    public string? CustomerCompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? BillAddrLine1 { get; set; }
    public string? BillAddrCity { get; set; }
    public string? BillAddrPostalCode { get; set; }
    public string? BillAddrCountrySubDivisionCode { get; set; }
    public bool Active { get; set; }
}

public class CreateCustomerRequest
{
    public string RealmId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? GivenName { get; set; }
    public string? MiddleName { get; set; }
    public string? FamilyName { get; set; }
    public string? Title { get; set; }
    public string? Suffix { get; set; }
    public string? CompanyName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Mobile { get; set; }
    public string? BillAddrLine1 { get; set; }
    public string? BillAddrCity { get; set; }
    public string? BillAddrPostalCode { get; set; }
    public string? BillAddrCountrySubDivisionCode { get; set; }
}

public class UpdateCustomerRequest : CreateCustomerRequest
{
    public string SyncToken { get; set; } = string.Empty;
}
