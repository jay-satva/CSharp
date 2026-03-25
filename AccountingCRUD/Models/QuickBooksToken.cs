using System;

namespace AccountingCRUD.Models
{
    public class QuickBooksToken
    {
        public string UserId { get; set; }
        public string AccessToken { get; set; }
        public string IdToken { get; set; }
        public string RefreshToken { get; set; }
        public string RealmId { get; set; }
        public DateTime AccessTokenExpiry { get; set; }
        public DateTime RefreshTokenExpiry { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
