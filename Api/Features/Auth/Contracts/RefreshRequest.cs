using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Contracts;

public sealed class RefreshRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
