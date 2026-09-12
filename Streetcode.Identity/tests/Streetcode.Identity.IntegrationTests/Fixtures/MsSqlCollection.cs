namespace Streetcode.Identity.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class MsSqlCollection : ICollectionFixture<MsSqlContainerFixture>
{
    public const string Name = "MsSql integration tests";
}
