using System.ComponentModel.DataAnnotations;

namespace JobHubPro.Api.DTOs.Auth;

public class RegisterDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = null!;

    [Required, MinLength(6)]
    public string Password { get; set; } = null!;

    /// <summary>CANDIDATE | EMPLOYER</summary>
    [Required]
    public string Role { get; set; } = "CANDIDATE";
}
