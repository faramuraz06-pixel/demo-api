using Microsoft.AspNetCore.Mvc.Testing;

namespace DemoApi.Tests.Integration;

// Один экземпляр API на все интеграционные тесты; тесты коллекции выполняются последовательно
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<WebApplicationFactory<Program>>
{
    public const string Name = "api";
}
