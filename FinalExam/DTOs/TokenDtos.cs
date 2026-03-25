namespace FinalExam.DTOs;

public class TokenResponseDto
{
    public string access_token { get; set; } = null!;
    public string token_type { get; set; } = null!;
    public long expires_in { get; set; }
    public string refresh_token { get; set; } = null!;
    public long x_refresh_token_expires_in { get; set; }
    public string? id_token { get; set; }
}

public class UserInfoDto
{
    public string sub { get; set; } = null!;
    public string? name { get; set; }
    public string? givenName { get; set; }
    public string? familyName { get; set; }
    public string? email { get; set; }
    public bool email_verified { get; set; }
}
