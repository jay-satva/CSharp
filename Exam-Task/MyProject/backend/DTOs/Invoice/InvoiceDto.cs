namespace MyProject.Application.DTOs.Invoice
{
    public class InvoiceDto
    {
        public int Id { get; set; }
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public string QuickBooksInvoiceId { get; set; } = string.Empty;
        public string CustomerRef { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? Memo { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<InvoiceLineItemDto> LineItems { get; set; } = new();
    }
}
