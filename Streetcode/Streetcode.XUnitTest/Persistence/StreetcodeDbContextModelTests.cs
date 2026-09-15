// <copyright file="StreetcodeDbContextModelTests.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
namespace Streetcode.XUnitTest.Persistence
{
    using Microsoft.EntityFrameworkCore;
    using Streetcode.DAL.Entities.Partners;
    using Streetcode.DAL.Entities.Streetcode;
    using Streetcode.DAL.Entities.Timeline;
    using Streetcode.DAL.Persistence;
    using Xunit;

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

        [Fact]
        public void Model_WhenBuilt_ShouldConfigureHistoricalContextTitleAsUnique()
        {
            var options = new DbContextOptionsBuilder<StreetcodeDbContext>()
                .UseSqlServer(
                    "Server=.;Database=Test;")
                .Options;

            using var context = new StreetcodeDbContext(options);

            var entityType = context.Model.FindEntityType(typeof(HistoricalContext));

            Assert.NotNull(entityType);

            var titleIndex = entityType.GetIndexes()
                .Single(index => index.Properties.Any(
                    property =>
                        property.Name == nameof(HistoricalContext.Title)));

            Assert.True(titleIndex.IsUnique);
        }

        [Fact]
        public void Model_WhenBuilt_ShouldConfigureTimelineTextLengthLimits()
        {
            var options = new DbContextOptionsBuilder<StreetcodeDbContext>()
                .UseSqlServer(
                    "Server=.;Database=Test;")
                .Options;

            using var context = new StreetcodeDbContext(options);

            var timelineEntityType = context.Model.FindEntityType(
                typeof(TimelineItem));
            var historicalContextEntityType = context.Model.FindEntityType(
                typeof(HistoricalContext));

            Assert.NotNull(timelineEntityType);
            Assert.NotNull(historicalContextEntityType);

            var timelineTitle = timelineEntityType.FindProperty(
                nameof(TimelineItem.Title));
            var timelineDescription = timelineEntityType.FindProperty(
                nameof(TimelineItem.Description));
            var historicalContextTitle = historicalContextEntityType.FindProperty(
                nameof(HistoricalContext.Title));

            Assert.NotNull(timelineTitle);
            Assert.NotNull(timelineDescription);
            Assert.NotNull(historicalContextTitle);
            Assert.Equal(
                TimelineItem.TitleMaxLength,
                timelineTitle.GetMaxLength());
            Assert.Equal(
                TimelineItem.DescriptionMaxLength,
                timelineDescription.GetMaxLength());
            Assert.Equal(
                HistoricalContext.TitleMaxLength,
                historicalContextTitle.GetMaxLength());
        }

        [Fact]
        public void Model_WhenBuilt_ShouldCascadeDeleteHistoricalContextTimelineRelations()
        {
            var options = new DbContextOptionsBuilder<StreetcodeDbContext>()
                .UseSqlServer(
                    "Server=.;Database=Test;")
                .Options;

            using var context = new StreetcodeDbContext(options);

            var relationEntityType = context.Model.FindEntityType(
                typeof(HistoricalContextTimeline));

            Assert.NotNull(relationEntityType);

            var foreignKeys = relationEntityType.GetForeignKeys();
            var timelineForeignKey = foreignKeys.Single(
                foreignKey => foreignKey.Properties.Any(
                    property => property.Name ==
                        nameof(HistoricalContextTimeline.TimelineId)));
            var historicalContextForeignKey = foreignKeys.Single(
                foreignKey => foreignKey.Properties.Any(
                    property => property.Name ==
                        nameof(HistoricalContextTimeline.HistoricalContextId)));

            Assert.Equal(DeleteBehavior.Cascade, timelineForeignKey.DeleteBehavior);
            Assert.Equal(
                DeleteBehavior.Cascade,
                historicalContextForeignKey.DeleteBehavior);
        }
    }

    [Fact]
    public void Model_WhenBuilt_ShouldConfigureCommentStorage()
    {
        var options = new DbContextOptionsBuilder<StreetcodeDbContext>()
            .UseSqlServer(
                "Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeDbContext(options);

        var entityType = context.Model.FindEntityType(typeof(Comment));

        Assert.NotNull(entityType);
        Assert.Equal("comments", entityType.GetTableName());
        Assert.Equal("streetcode", entityType.GetSchema());

        var textProperty = entityType.FindProperty(nameof(Comment.Text));
        var updatedAtProperty = entityType.FindProperty(nameof(Comment.UpdatedAt));

        var parentCommentIdProperty = entityType.FindProperty(nameof(Comment.ParentCommentId));

        Assert.NotNull(textProperty);
        Assert.False(textProperty.IsNullable);
        Assert.Equal(Comment.TextMaxLength, textProperty.GetMaxLength());
        Assert.NotNull(updatedAtProperty);
        Assert.True(updatedAtProperty.IsNullable);
        Assert.NotNull(parentCommentIdProperty);
        Assert.True(parentCommentIdProperty.IsNullable);

        var streetcodeForeignKey = entityType.GetForeignKeys().Single(
            foreignKey => foreignKey.Properties.Any(
                property => property.Name == nameof(Comment.StreetcodeId)));

        Assert.Equal(typeof(StreetcodeContent), streetcodeForeignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Cascade, streetcodeForeignKey.DeleteBehavior);

        var parentCommentForeignKey = entityType.GetForeignKeys().Single(
            foreignKey => foreignKey.Properties.Any(
                property => property.Name == nameof(Comment.ParentCommentId)));

        Assert.Equal(typeof(Comment), parentCommentForeignKey.PrincipalEntityType.ClrType);
        Assert.Equal(DeleteBehavior.Restrict, parentCommentForeignKey.DeleteBehavior);
    }
}
