namespace MyProject.Application.DTOs.Item
{
    public class CreateItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public string Type { get; set; } = "Service";
        public string IncomeAccountRef { get; set; } = string.Empty;
    }
}