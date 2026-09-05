namespace Streetcode.Identity.WebApi.DTOs;

public sealed class RefreshSessionRequestDto
{
    public string RefreshToken { get; set; } = string.Empty;
}
