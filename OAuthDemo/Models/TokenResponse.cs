namespace OAuthDemo.Models
{
    public class TokenResponse
    {
        public string access_token { get; set; }
        public string refresh_token { get; set; }
        public int expires_in { get; set; }
        public int x_refresh_token_expires_in { get; set; }
        public string id_token { get; set; }
    }
}
