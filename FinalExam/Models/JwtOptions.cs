namespace FinalExam.Models;

public class JwtOptions
{
    public string Secret { get; set; } = null!;
    public string Issuer { get; set; } = null!;
    public string Audience { get; set; } = null!;
    public int ExpiryInMinutes { get; set; }
    public int OAuthStateExpiryInMinutes { get; set; } = 10;
}
