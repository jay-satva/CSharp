namespace MyProject.Application.DTOs.Invoice
{
    public class CreateInvoiceDto
    {
        public string CompanyId { get; set; } = string.Empty;
        public string CustomerRef { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? Memo { get; set; }
        public List<CreateInvoiceLineItemDto> LineItems { get; set; } = new();
    }

    public class CreateInvoiceLineItemDto
    {
        public string ItemRef { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}
