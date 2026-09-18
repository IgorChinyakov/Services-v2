using DirectoryService.Presentation.Extensions;
using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DirectoryService.IntegrationTests;

public sealed class CacheRegistrationTests
{
    [Fact]
    public void AddApplicationServices_WhenRedisIsDisabled_ShouldRegisterOnlyLocalHybridCache()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(redisEnabled: false);

        // Act
        services.AddApplicationServices(configuration);

        // Assert
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(HybridCache));
        services.Should().NotContain(descriptor => descriptor.ServiceType == typeof(IDistributedCache));
    }

    [Fact]
    public void AddApplicationServices_WhenRedisIsEnabled_ShouldRegisterDistributedCache()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = CreateConfiguration(redisEnabled: true);

        // Act
        services.AddApplicationServices(configuration);

        // Assert
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(HybridCache));
        services.Should().Contain(descriptor => descriptor.ServiceType == typeof(IDistributedCache));
    }

    private static IConfiguration CreateConfiguration(bool redisEnabled)
    {
        Dictionary<string, string?> values = new()
        {
            ["Redis:Enabled"] = redisEnabled.ToString(),
            ["Redis:ConnectionString"] = "localhost:6379",
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
