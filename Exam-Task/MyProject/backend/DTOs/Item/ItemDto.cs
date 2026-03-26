namespace MyProject.Application.DTOs.Item
{
    public class ItemDto
    {
        public string Id { get; set; } = string.Empty;
        public string CompanyId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal UnitPrice { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool Active { get; set; }
    }
}
