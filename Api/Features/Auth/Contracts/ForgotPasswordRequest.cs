using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Contracts;

public sealed class ForgotPasswordRequest
{
    [Required]
    [MaxLength(255)]
    public string Identifier { get; set; } = string.Empty;
}
