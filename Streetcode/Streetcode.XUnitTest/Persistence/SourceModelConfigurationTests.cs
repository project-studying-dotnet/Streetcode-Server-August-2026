using Microsoft.EntityFrameworkCore;
using Streetcode.DAL.Persistence;
using Xunit;
using SourceCategoryEntity =
    Streetcode.DAL.Entities.Sources.SourceLinkCategory;
using SourceContentEntity =
    Streetcode.DAL.Entities.Sources.StreetcodeCategoryContent;

namespace Streetcode.XUnitTest.Persistence;

public class SourceModelConfigurationTests
{
    [Fact]
    public void Model_WhenBuilt_ShouldConfigureSourcePropertyLimits()
    {
        using var context = CreateContext();

        var categoryEntityType = context.Model.FindEntityType(
            typeof(SourceCategoryEntity));
        var contentEntityType = context.Model.FindEntityType(
            typeof(SourceContentEntity));

        Assert.NotNull(categoryEntityType);
        Assert.NotNull(contentEntityType);

        var titleProperty = categoryEntityType.FindProperty(
            nameof(SourceCategoryEntity.Title));
        var imageHashProperty = categoryEntityType.FindProperty(
            nameof(SourceCategoryEntity.ImageHash));
        var textProperty = contentEntityType.FindProperty(
            nameof(SourceContentEntity.Text));

        Assert.NotNull(titleProperty);
        Assert.NotNull(imageHashProperty);
        Assert.NotNull(textProperty);
        Assert.Equal(
            SourceCategoryEntity.TitleMaxLength,
            titleProperty.GetMaxLength());
        Assert.Equal(
            SourceCategoryEntity.ImageHashLength,
            imageHashProperty.GetMaxLength());
        Assert.True(imageHashProperty.IsNullable);
        Assert.Equal(
            SourceContentEntity.TextMaxLength,
            textProperty.GetMaxLength());
    }

    [Fact]
    public void Model_WhenBuilt_ShouldConfigureUniqueSourceCategoryIndexes()
    {
        using var context = CreateContext();

        var categoryEntityType = context.Model.FindEntityType(
            typeof(SourceCategoryEntity));

        Assert.NotNull(categoryEntityType);

        var indexes = categoryEntityType.GetIndexes().ToList();

        var titleIndex = indexes.Single(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(SourceCategoryEntity.Title));
        var imageIdIndex = indexes.Single(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(SourceCategoryEntity.ImageId));
        var imageHashIndex = indexes.Single(index =>
            index.Properties.Count == 1 &&
            index.Properties[0].Name == nameof(SourceCategoryEntity.ImageHash));

        Assert.True(titleIndex.IsUnique);
        Assert.True(imageIdIndex.IsUnique);
        Assert.True(imageHashIndex.IsUnique);
        Assert.Equal(
            "[ImageHash] IS NOT NULL",
            imageHashIndex.GetFilter());
    }

    private static StreetcodeDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<StreetcodeDbContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .Options;

        return new StreetcodeDbContext(options);
    }
}
