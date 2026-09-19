using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Streetcode.Identity.Domain.RefreshTokens;
using Streetcode.Identity.Infrastructure.Identity;
using Streetcode.Identity.Infrastructure.Persistence;
using Streetcode.Identity.Infrastructure.Persistence.Outbox;

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

    [Fact]
    public void Model_WhenBuilt_ShouldConfigureOutboxMessage()
    {
        var options = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeIdentityDbContext(options);
        var entityType = Assert.IsAssignableFrom<IEntityType>(
            context.Model.FindEntityType(typeof(OutboxMessage)));

        Assert.Equal("OutboxMessages", entityType.GetTableName());

        var primaryKey = Assert.IsAssignableFrom<IKey>(entityType.FindPrimaryKey());
        Assert.Equal(
            nameof(OutboxMessage.Id),
            Assert.Single(primaryKey.Properties).Name);

        var idProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(OutboxMessage.Id)));
        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);

        var typeProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(OutboxMessage.Type)));
        Assert.False(typeProperty.IsNullable);
        Assert.Equal(200, typeProperty.GetMaxLength());

        var keyProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(OutboxMessage.Key)));
        Assert.False(keyProperty.IsNullable);
        Assert.Equal(100, keyProperty.GetMaxLength());

        var payloadProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(OutboxMessage.Payload)));
        Assert.False(payloadProperty.IsNullable);
        Assert.Equal("nvarchar(max)", payloadProperty.GetColumnType());

        var occurredAtProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(OutboxMessage.OccurredAt)));
        Assert.False(occurredAtProperty.IsNullable);

        var processedAtProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(OutboxMessage.ProcessedAt)));
        Assert.True(processedAtProperty.IsNullable);

        var retryCountProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(OutboxMessage.RetryCount)));
        Assert.Equal(0, Assert.IsType<int>(retryCountProperty.GetDefaultValue()));

        var lastErrorProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(OutboxMessage.LastError)));
        Assert.True(lastErrorProperty.IsNullable);
        Assert.Equal(2000, lastErrorProperty.GetMaxLength());

        var pendingMessagesIndex = entityType.GetIndexes()
            .Single(index =>
                index.Properties.Select(property => property.Name)
                    .SequenceEqual(
                    [
                        nameof(OutboxMessage.ProcessedAt),
                        nameof(OutboxMessage.OccurredAt)
                    ]));

        Assert.Equal(
            "IX_OutboxMessages_ProcessedAt_OccurredAt",
            pendingMessagesIndex.GetDatabaseName());
    }

    [Fact]
    public void Model_WhenBuilt_ShouldConfigureRefreshTokenPropertiesAndIndexes()
    {
        var options = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeIdentityDbContext(options);
        var entityType = Assert.IsAssignableFrom<IEntityType>(
            context.Model.FindEntityType(typeof(RefreshToken)));

        Assert.Equal("RefreshTokens", entityType.GetTableName());

        var primaryKey = Assert.IsAssignableFrom<IKey>(
            entityType.FindPrimaryKey());
        Assert.Equal(
            nameof(RefreshToken.Id),
            Assert.Single(primaryKey.Properties).Name);

        var idProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(RefreshToken.Id)));
        Assert.Equal(ValueGenerated.Never, idProperty.ValueGenerated);

        var tokenHashProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(nameof(RefreshToken.TokenHash)));
        Assert.False(tokenHashProperty.IsNullable);
        Assert.False(tokenHashProperty.IsUnicode());
        Assert.Equal(64, tokenHashProperty.GetMaxLength());

        var tokenHashIndex = entityType.GetIndexes()
            .Single(index =>
                index.GetDatabaseName() ==
                "UX_RefreshTokens_TokenHash");

        Assert.True(tokenHashIndex.IsUnique);
        Assert.Equal(
            nameof(RefreshToken.TokenHash),
            Assert.Single(tokenHashIndex.Properties).Name);

        var indexNames = entityType.GetIndexes()
            .Select(index => index.GetDatabaseName())
            .ToList();

        Assert.Contains("IX_RefreshTokens_UserId", indexNames);
        Assert.Contains("IX_RefreshTokens_FamilyId", indexNames);
    }

    [Fact]
    public void Model_WhenBuilt_ShouldConfigureRefreshTokenConcurrencyVersion()
    {
        var options = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeIdentityDbContext(options);
        var entityType = Assert.IsAssignableFrom<IEntityType>(
            context.Model.FindEntityType(typeof(RefreshToken)));

        var concurrencyProperty = Assert.IsAssignableFrom<IProperty>(
            entityType.FindProperty(
                nameof(RefreshToken.ConcurrencyVersion)));

        Assert.False(concurrencyProperty.IsNullable);
        Assert.True(concurrencyProperty.IsConcurrencyToken);
        Assert.Equal(
            1L,
            Assert.IsType<long>(
                concurrencyProperty.GetDefaultValue()));
    }

    [Fact]
    public void Model_WhenBuilt_ShouldConfigureRefreshTokenRelationships()
    {
        var options = new DbContextOptionsBuilder<StreetcodeIdentityDbContext>()
            .UseSqlServer("Server=.;Database=Test;")
            .Options;

        using var context = new StreetcodeIdentityDbContext(options);

        var entityType = Assert.IsAssignableFrom<IEntityType>(
            context.Model.FindEntityType(typeof(RefreshToken)));

        var userForeignKey = entityType.GetForeignKeys()
            .Single(foreignKey =>
                foreignKey.Properties.Count == 1 &&
                foreignKey.Properties[0].Name ==
                nameof(RefreshToken.UserId));

        Assert.Equal(
            typeof(ApplicationUser),
            userForeignKey.PrincipalEntityType.ClrType);

        Assert.Equal(
            DeleteBehavior.Cascade,
            userForeignKey.DeleteBehavior);

        var replacementForeignKey = entityType.GetForeignKeys()
            .Single(foreignKey =>
                foreignKey.Properties.Count == 1 &&
                foreignKey.Properties[0].Name ==
                nameof(RefreshToken.ReplacedByTokenId));

        Assert.Equal(
            typeof(RefreshToken),
            replacementForeignKey.PrincipalEntityType.ClrType);

        Assert.Equal(
            DeleteBehavior.NoAction,
            replacementForeignKey.DeleteBehavior);
    }
}
