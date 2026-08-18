using Microsoft.EntityFrameworkCore;
using Streetcode.DAL.Entities.Partners;
using Streetcode.DAL.Entities.Streetcode;
using Streetcode.DAL.Persistence;
using Xunit;

namespace Streetcode.XUnitTest.Persistence;

public class StreetcodeDbContextModelTests
{
    [Fact]
    public void Model_WhenBuilt_ShouldConfigureRelatedFigureDeleteBehaviors()
    {
        var options = new DbContextOptionsBuilder<StreetcodeDbContext>()
            .UseSqlServer(
                "Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(RelatedFigure));

        Assert.NotNull(entityType);

        var foreignKeys = entityType.GetForeignKeys();

        var observerForeignKey = foreignKeys.Single(
            foreignKey => foreignKey.Properties.Any(
                property => property.Name == nameof(RelatedFigure.ObserverId)));

        var targetForeignKey = foreignKeys.Single(
            foreignKey => foreignKey.Properties.Any(
                property => property.Name == nameof(RelatedFigure.TargetId)));

        Assert.Equal(DeleteBehavior.Restrict, observerForeignKey.DeleteBehavior);
        Assert.Equal(DeleteBehavior.Cascade, targetForeignKey.DeleteBehavior);
    }

    [Fact]
    public void Model_WhenBuilt_ShouldConfigurePartnerDefaultValueAsBooleanFalse()
    {
        var options = new DbContextOptionsBuilder<StreetcodeDbContext>()
            .UseSqlServer(
                "Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Partner));

        Assert.NotNull(entityType);

        var property = entityType.FindProperty(nameof(Partner.IsKeyPartner));

        Assert.NotNull(property);

        var defaultValue = Assert.IsType<bool>(property.GetDefaultValue());

        Assert.False(defaultValue);
    }
}
