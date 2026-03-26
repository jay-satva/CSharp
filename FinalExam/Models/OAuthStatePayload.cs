namespace FinalExam.Models;

public class OAuthStatePayload
{
    public string? UserId { get; set; }
    public string Mode { get; set; } = null!;
}
