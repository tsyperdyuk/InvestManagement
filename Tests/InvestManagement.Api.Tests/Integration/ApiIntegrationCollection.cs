namespace InvestManagement.Api.Tests.Integration;

[CollectionDefinition(Name)]
public sealed class ApiIntegrationCollection : ICollectionFixture<InvestManagementApiFactory>
{
    public const string Name = "API integration tests";
}
