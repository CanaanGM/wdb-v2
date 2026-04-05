using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Contracts;

public sealed class ResetPasswordRequest
{
    [Required]
    [MaxLength(255)]
    public string Identifier { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [MaxLength(200)]
    public string NewPassword { get; set; } = string.Empty;
}
