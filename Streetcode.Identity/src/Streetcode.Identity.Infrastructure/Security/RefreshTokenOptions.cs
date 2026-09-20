namespace Streetcode.Identity.Infrastructure.Security;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshTokens";

    public TimeSpan Lifetime { get; init; }

    public TimeSpan RotationGracePeriod { get; init; }
}
