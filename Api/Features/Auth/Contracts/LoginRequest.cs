using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Contracts;

public sealed class LoginRequest
{
    [Required]
    [MaxLength(255)]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}
