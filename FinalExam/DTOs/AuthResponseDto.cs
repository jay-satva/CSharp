namespace FinalExam.DTOs;

public class AuthResponseDto
{
    public string AccessToken { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Email { get; set; }
    public string? IntuitSub { get; set; }
}
