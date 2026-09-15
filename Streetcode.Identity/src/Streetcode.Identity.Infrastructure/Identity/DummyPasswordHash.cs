namespace Streetcode.Identity.Infrastructure.Identity;

public sealed class DummyPasswordHash
{
    public string Value { get; }

    public DummyPasswordHash(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);

        Value = value;
    }
}
