namespace FinalExam.DTOs;

public class InvoiceDto
{
    public int Id { get; set; }
    public string RealmId { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string QuickBooksInvoiceId { get; set; } = string.Empty;
    public string CustomerRef { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Memo { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<InvoiceLineItemDto> LineItems { get; set; } = new();
}

public class InvoiceLineItemDto
{
    public int Id { get; set; }
    public string ItemRef { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}

public class CreateInvoiceRequest
{
    public string RealmId { get; set; } = string.Empty;
    public string CustomerRef { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? AccountRef { get; set; }
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Memo { get; set; }
    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();
}

public class CreateInvoiceLineItemRequest
{
    public string ItemRef { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class UpdateInvoiceRequest
{
    public string CustomerRef { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; }
    public DateTime? DueDate { get; set; }
    public string? Memo { get; set; }
    public List<CreateInvoiceLineItemRequest> LineItems { get; set; } = new();
}
