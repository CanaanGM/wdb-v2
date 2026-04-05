using System.ComponentModel.DataAnnotations;

namespace Api.Features.Auth.Contracts;

public sealed class LogoutRequest
{
    [Required]
    public string RefreshToken { get; set; } = string.Empty;
}
