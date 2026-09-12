namespace Streetcode.Identity.Infrastructure.Identity.Seeding;

public sealed class IdentitySeedOptions
{
    public const string SectionName = "IdentitySeed";

    public bool Enabled { get; init; }

    public string AdminEmail { get; init; } = string.Empty;

    public string AdminPassword { get; init; } = string.Empty;
}
