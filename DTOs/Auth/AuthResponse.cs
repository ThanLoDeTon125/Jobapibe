namespace JobHubPro.Api.DTOs.Auth;

public class AuthResponse
{
    public string Token { get; set; } = null!;
    public string Role { get; set; } = null!;
    public int UserId { get; set; }
    public string Email { get; set; } = null!;
}
