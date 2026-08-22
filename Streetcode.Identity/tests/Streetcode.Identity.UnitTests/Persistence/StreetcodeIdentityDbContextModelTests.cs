using Microsoft.EntityFrameworkCore;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.Infrastructure.Persistence;

namespace Streetcode.Identity.UnitTests.Persistence;

public class StreetcodeIdentityDbContextModelTests
{
    [Fact]
    public void Model_WhenBuilt_ShouldConfigureApplicationUserIsActiveDefaultAsTrue()
    {
        var optionsBuilder = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>();
        var options = optionsBuilder
            .UseSqlServer("Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeIdentityDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(ApplicationUser));

        Assert.NotNull(entityType);
        var property = entityType.FindProperty(nameof(ApplicationUser.IsActive));
        Assert.NotNull(property);

        var defaultValue = Assert.IsType<bool>(property.GetDefaultValue());
        Assert.True(defaultValue);
    }

    [Fact]
    public void Model_WhenBuilt_ShouldConfigureApplicationUserAccessVersionDefaultAndConcurrencyToken()
    {
        var optionsBuilder = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>();
        var options = optionsBuilder
            .UseSqlServer("Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeIdentityDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(ApplicationUser));

        Assert.NotNull(entityType);
        var property = entityType.FindProperty(nameof(ApplicationUser.AccessVersion));
        Assert.NotNull(property);

        var defaultValue = Assert.IsType<long>(property.GetDefaultValue());
        Assert.Equal(1L, defaultValue);
        Assert.True(property.IsConcurrencyToken);
    }

    [Fact]
    public void Model_WhenBuilt_ShouldRequireUniqueNormalizedEmail()
    {
        var optionsBuilder = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>();
        var options = optionsBuilder
            .UseSqlServer("Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeIdentityDbContext(options);
        var entityType = context.Model.FindEntityType(typeof(ApplicationUser));

        Assert.NotNull(entityType);

        var emailProperty = entityType.FindProperty(nameof(ApplicationUser.Email));
        var normalizeEmailProperty = entityType.FindProperty(nameof(ApplicationUser.NormalizedEmail));

        Assert.NotNull(emailProperty);
        Assert.NotNull(normalizeEmailProperty);

        Assert.False(emailProperty.IsNullable);
        Assert.False(normalizeEmailProperty.IsNullable);

        var emailIndex = entityType.GetIndexes()
            .Single(index =>
                index.Properties.Count == 1 &&
                index.Properties[0].Name == nameof(ApplicationUser.NormalizedEmail));

        Assert.True(emailIndex.IsUnique);
        Assert.Equal("EmailIndex", emailIndex.GetDatabaseName());
    }
}
