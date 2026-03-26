namespace FinalExam.DTOs;

public class ItemDto
{
    public string Id { get; set; } = string.Empty;
    public string SyncToken { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? QtyOnHand { get; set; }
    public string? Type { get; set; }
    public string? IncomeAccountName { get; set; }
    public bool Active { get; set; }
}

public class CreateItemRequest
{
    public string RealmId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal? QtyOnHand { get; set; }
    public string? Type { get; set; }
    public string? IncomeAccountRef { get; set; }
    public string? ExpenseAccountRef { get; set; }
    public string? AssetAccountRef { get; set; }
}

public class UpdateItemRequest : CreateItemRequest
{
    public string SyncToken { get; set; } = string.Empty;
}
