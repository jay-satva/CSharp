namespace MyProject.Application.DTOs.QuickBooks
{
    public class CompanyDto
    {
        public string Id { get; set; } = string.Empty;
        public string RealmId { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
        public DateTime ConnectedAt { get; set; }
    }
}