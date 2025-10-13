using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth;

public class RegisterDto
{
    [Required] [EmailAddress] public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(3)]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required] [MinLength(6)] public string Password { get; set; } = string.Empty;

    [Compare("Password")] public string ConfirmPassword { get; set; } = string.Empty;
}