using Intropy.IdempotencyService.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Intropy.IdempotencyService.Test;

public class IdempotencyConfigTests
{
    [Fact]
    public void RecordTtl_WhenNotConfigured_DefaultsToThirtyDays()
    {
        // Arrange
        var provider = BuildProvider();

        // Act
        var config = provider.GetRequiredService<IOptions<IdempotencyConfig>>().Value;

        // Assert
        Assert.Equal(TimeSpan.FromDays(30), config.RecordTtl);
    }

    [Theory]
    [InlineData("30.00:00:00", 30, 0, 0, 0)]
    [InlineData("12:00:00", 0, 12, 0, 0)]
    [InlineData("1.06:30:00", 1, 6, 30, 0)]
    public void RecordTtl_WhenConfigured_BindsFromConfiguration(string configured, int days, int hours, int minutes,
        int seconds)
    {
        // Arrange
        var provider = BuildProvider(configured);

        // Act
        var config = provider.GetRequiredService<IOptions<IdempotencyConfig>>().Value;

        // Assert
        Assert.Equal(new TimeSpan(days, hours, minutes, seconds), config.RecordTtl);
    }

    [Theory]
    [InlineData("00:00:00")]
    [InlineData("-1.00:00:00")]
    public void RecordTtl_WhenNotGreaterThanZero_FailsValidation(string configured)
    {
        // Arrange
        var provider = BuildProvider(configured);

        // Act
        var exception = Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<IdempotencyConfig>>().Value);

        // Assert
        Assert.Contains("IdempotencyConfig:RecordTtl must be greater than zero.", exception.Failures);
    }

    private static ServiceProvider BuildProvider(string? recordTtl = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(recordTtl is null
                ? []
                : [new KeyValuePair<string, string?>("IdempotencyConfig:RecordTtl", recordTtl)])
            .Build();

        var services = new ServiceCollection();
        services.AddOptions<IdempotencyConfig>()
            .Bind(configuration.GetSection("IdempotencyConfig"))
            .Validate(c => c.RecordTtl > TimeSpan.Zero, "IdempotencyConfig:RecordTtl must be greater than zero.")
            .ValidateOnStart();

        return services.BuildServiceProvider();
    }
}
