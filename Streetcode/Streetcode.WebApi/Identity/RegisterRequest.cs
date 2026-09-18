using System.ComponentModel.DataAnnotations;

namespace Streetcode.WebApi.Identity;

public sealed class RegisterRequest
{
    [Required]
    [MaxLength(50)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Surname { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;
}
