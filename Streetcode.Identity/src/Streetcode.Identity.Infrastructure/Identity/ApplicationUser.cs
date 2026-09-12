using Microsoft.AspNetCore.Identity;

namespace Streetcode.Identity.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; private set; } = true;

    public long AccessVersion { get; private set; } = 1;

    public bool Activate()
    {
        if (IsActive)
        {
            return false;
        }

        IsActive = true;
        IncrementAccessVersion();

        return true;
    }

    public bool Deactivate()
    {
        if (!IsActive)
        {
            return false;
        }

        IsActive = false;
        IncrementAccessVersion();

        return true;
    }

    public void IncrementAccessVersion()
    {
        AccessVersion = checked(AccessVersion + 1);
    }
}
