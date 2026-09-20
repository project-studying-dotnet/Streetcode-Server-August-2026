namespace Streetcode.Email.IntegrationTests.Fixtures;

[CollectionDefinition(Name)]
public sealed class EmailInfrastructureCollection
    : ICollectionFixture<EmailInfrastructureFixture>
{
    public const string Name = "Email infrastructure";
}
