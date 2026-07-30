namespace DirectoryService.IntegrationTests;

[CollectionDefinition(Name)]
public sealed class DirectoryServiceTestCollection : ICollectionFixture<DirectoryServiceWebFactory>
{
    public const string Name = "DirectoryService integration tests";
}
