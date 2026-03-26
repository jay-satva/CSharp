namespace MyProject.Application.DTOs.Invoice
{
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
}