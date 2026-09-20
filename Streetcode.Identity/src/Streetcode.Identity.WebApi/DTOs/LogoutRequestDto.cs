namespace Streetcode.Identity.WebApi.DTOs;

public sealed class LogoutRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
