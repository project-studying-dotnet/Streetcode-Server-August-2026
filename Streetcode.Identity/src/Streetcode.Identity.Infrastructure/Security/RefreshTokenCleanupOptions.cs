namespace Streetcode.Identity.Infrastructure.Security;

public sealed class RefreshTokenCleanupOptions
{
    public const string SectionName =
        RefreshTokenOptions.SectionName + ":Cleanup";

    public TimeSpan Interval { get; init; }

    public TimeSpan RetentionPeriod { get; init; }

    public int BatchSize { get; init; }
}
