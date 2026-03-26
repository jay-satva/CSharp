namespace FinalExam.Models;

public class InvoiceRecord
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string RealmId { get; set; } = string.Empty;
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
    public List<InvoiceLineItemRecord> LineItems { get; set; } = new();
}

public class InvoiceLineItemRecord
{
    public int Id { get; set; }
    public int InvoiceId { get; set; }
    public string ItemRef { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Amount { get; set; }
}
