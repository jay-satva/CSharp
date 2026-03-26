namespace MyProject.Domain.Entities
{
    public class InvoiceLineItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string ItemRef { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Amount { get; set; }
        public Invoice Invoice { get; set; } = null!;
    }
}