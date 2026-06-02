using Dapr.Client;
using Intropy.Contracts.IdempotencyService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Intropy.IdempotencyService.Client.Test;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddIdempotencyServiceClient_WithoutOptions_RegistersServicesWithDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdempotencyServiceClient();
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<IdempotencyServiceClientOptions>>();
        Assert.NotNull(options);
        Assert.Equal("idempotency-service", options.Value.AppId);
        Assert.Null(options.Value.DaprHttpEndpoint);

        var daprClient = serviceProvider.GetService<DaprClient>();
        Assert.NotNull(daprClient);

        var client = serviceProvider.GetService<IIdempotencyServiceClient>();
        Assert.NotNull(client);
        Assert.IsType<IdempotencyServiceClient>(client);
    }

    [Fact]
    public void AddIdempotencyServiceClient_WithAction_ConfiguresOptions()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdempotencyServiceClient(options =>
        {
            options.AppId = "custom-app-id";
            options.DaprHttpEndpoint = "http://localhost:4000";
        });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<IdempotencyServiceClientOptions>>();
        Assert.NotNull(options);
        Assert.Equal("custom-app-id", options.Value.AppId);
        Assert.Equal("http://localhost:4000", options.Value.DaprHttpEndpoint);
    }

    [Fact]
    public void AddIdempotencyServiceClient_WithNullAction_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdempotencyServiceClient(configureOptions: null);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<IdempotencyServiceClientOptions>>();
        Assert.NotNull(options);
        Assert.Equal("idempotency-service", options.Value.AppId);
        Assert.Null(options.Value.DaprHttpEndpoint);
    }

    [Fact]
    public void AddIdempotencyServiceClient_RegistersDaprClientAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdempotencyServiceClient();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var client1 = serviceProvider.GetService<DaprClient>();
        var client2 = serviceProvider.GetService<DaprClient>();

        // Assert
        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.Same(client1, client2);
    }

    [Fact]
    public void AddIdempotencyServiceClient_RegistersIdempotencyServiceClientAsSingleton()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdempotencyServiceClient();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var client1 = serviceProvider.GetService<IIdempotencyServiceClient>();
        var client2 = serviceProvider.GetService<IIdempotencyServiceClient>();

        // Assert
        Assert.NotNull(client1);
        Assert.NotNull(client2);
        Assert.Same(client1, client2);
    }

    [Fact]
    public void AddIdempotencyServiceClient_WithConfiguration_BindsOptionsFromConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>("IdempotencyService:AppId", "config-app-id"),
                new KeyValuePair<string, string?>("IdempotencyService:DaprHttpEndpoint", "http://localhost:5000")
            ])
            .Build();

        // Act
        services.AddIdempotencyServiceClient(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<IdempotencyServiceClientOptions>>();
        Assert.NotNull(options);
        Assert.Equal("config-app-id", options.Value.AppId);
        Assert.Equal("http://localhost:5000", options.Value.DaprHttpEndpoint);
    }

    [Fact]
    public void AddIdempotencyServiceClient_WithEmptyConfiguration_UsesDefaults()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([])
            .Build();

        // Act
        services.AddIdempotencyServiceClient(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<IdempotencyServiceClientOptions>>();
        Assert.NotNull(options);
        Assert.Equal("idempotency-service", options.Value.AppId);
        Assert.Null(options.Value.DaprHttpEndpoint);
    }

    [Fact]
    public void AddIdempotencyServiceClient_WithPartialConfiguration_UsesDefaultsForMissingValues()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection([
                new KeyValuePair<string, string?>("IdempotencyService:AppId", "partial-config-app")
            ])
            .Build();

        // Act
        services.AddIdempotencyServiceClient(configuration);
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var options = serviceProvider.GetService<IOptions<IdempotencyServiceClientOptions>>();
        Assert.NotNull(options);
        Assert.Equal("partial-config-app", options.Value.AppId);
        Assert.Null(options.Value.DaprHttpEndpoint);
    }

    [Fact]
    public void AddIdempotencyServiceClient_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddIdempotencyServiceClient();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddIdempotencyServiceClient_WithConfiguration_ReturnsServiceCollection_ForChaining()
    {
        // Arrange
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // Act
        var result = services.AddIdempotencyServiceClient(configuration);

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddIdempotencyServiceClient_RegistersCorrectServiceTypes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdempotencyServiceClient();

        // Assert
        var descriptors = services.ToList();
        Assert.Contains(descriptors, d =>
            d.ServiceType == typeof(IConfigureOptions<IdempotencyServiceClientOptions>));
        Assert.Contains(descriptors, d =>
            d.ServiceType == typeof(DaprClient) &&
            d.Lifetime == ServiceLifetime.Singleton);
        Assert.Contains(descriptors, d =>
            d.ServiceType == typeof(IIdempotencyServiceClient) &&
            d.ImplementationType == typeof(IdempotencyServiceClient) &&
            d.Lifetime == ServiceLifetime.Singleton);
    }

    [Fact]
    public void AddIdempotencyServiceClient_CanBeCalledMultipleTimes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdempotencyServiceClient(options => options.AppId = "first");
        services.AddIdempotencyServiceClient(options => options.AppId = "second");
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var client = serviceProvider.GetService<IIdempotencyServiceClient>();
        Assert.NotNull(client);
    }


    [Fact]
    public void AddIdempotencyServiceClient_WithEmptyStringEndpoint_TreatsAsNull()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddIdempotencyServiceClient(options => { options.DaprHttpEndpoint = string.Empty; });
        var serviceProvider = services.BuildServiceProvider();

        // Assert
        var daprClient = serviceProvider.GetService<DaprClient>();
        Assert.NotNull(daprClient);
    }

    [Fact]
    public void AddIdempotencyServiceClient_CreatesIdempotencyServiceClient_WithDaprClient()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddIdempotencyServiceClient();

        // Act
        var serviceProvider = services.BuildServiceProvider();
        var idempotencyClient = serviceProvider.GetService<IIdempotencyServiceClient>();
        var daprClient = serviceProvider.GetService<DaprClient>();

        // Assert
        Assert.NotNull(idempotencyClient);
        Assert.NotNull(daprClient);
    }
}
