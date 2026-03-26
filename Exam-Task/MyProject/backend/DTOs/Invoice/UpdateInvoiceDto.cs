namespace MyProject.Application.DTOs.Invoice
{
    public class UpdateInvoiceDto
    {
        public string CustomerRef { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public string? Memo { get; set; }
        public List<CreateInvoiceLineItemDto> LineItems { get; set; } = new();
    }
}