namespace MyProject.Domain.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public string QuickBooksInvoiceId { get; set; } = string.Empty;
        public string CustomerRef { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? Memo { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public List<InvoiceLineItem> LineItems { get; set; } = new();
    }
}