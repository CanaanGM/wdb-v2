using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Contracts;

public sealed class RegisterRequest
{
    [Required]
    [MaxLength(255)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
