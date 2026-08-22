using Streetcode.Identity.Infrastructure.Identity;

namespace Streetcode.Identity.UnitTests;

public class ApplicationUserTests
{
    [Fact]
    public void Constructor_WhenCalled_ShouldInitializeActiveUserWithFirstAccessVersion()
    {
        var user = new ApplicationUser();

        Assert.True(user.IsActive);
        Assert.Equal(1, user.AccessVersion);
    }

    [Fact]
    public void Deactivate_WhenUserIsActive_ShouldDeactivateAndIncrementAccessVersion()
    {
        var user = new ApplicationUser();
        var wasDeactivated = user.Deactivate();

        Assert.True(wasDeactivated);
        Assert.False(user.IsActive);
        Assert.Equal(2, user.AccessVersion);
    }

    [Fact]
    public void Deactivate_WhenUserIsAlreadyInactive_ShouldNotChangeStateOrAccessVersion()
    {
        var user = new ApplicationUser();
        user.Deactivate();

        var wasDeactivated = user.Deactivate();

        Assert.False(wasDeactivated);
        Assert.False(user.IsActive);
        Assert.Equal(2, user.AccessVersion);
    }

    [Fact]
    public void Activate_WhenUserIsInactive_ShouldActivateAndIncrementAccessVersion()
    {
        var user = new ApplicationUser();
        user.Deactivate();

        var wasActivated = user.Activate();

        Assert.True(wasActivated);
        Assert.True(user.IsActive);
        Assert.Equal(3, user.AccessVersion);
    }

    [Fact]
    public void Activate_WhenUserIsAlreadyActive_ShouldNotChangeStateOrAccessVersion()
    {
        var user = new ApplicationUser();
        var wasActivated = user.Activate();

        Assert.False(wasActivated);
        Assert.True(user.IsActive);
        Assert.Equal(1, user.AccessVersion);
    }

    [Fact]
    public void IncrementAccessVersion_WhenCalled_ShouldIncrementVersionWithoutChangingActiveState()
    {
        var user = new ApplicationUser();

        user.IncrementAccessVersion();

        Assert.Equal(2, user.AccessVersion);
        Assert.True(user.IsActive);
    }
}
