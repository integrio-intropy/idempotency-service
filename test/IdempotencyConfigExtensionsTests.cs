using Intropy.IdempotencyService.Configuration;
using Intropy.IdempotencyService.Extensions;

namespace Intropy.IdempotencyService.Test;

public class IdempotencyConfigExtensionsTests
{
    [Fact]
    public void ToStateMetadata_WithDefaultTtl_ReturnsThirtyDaysInSeconds()
    {
        // Arrange
        var config = new IdempotencyConfig();

        // Act
        var metadata = config.ToStateMetadata();

        // Assert
        Assert.Equal("2592000", Assert.Contains("ttlInSeconds", metadata));
    }

    [Fact]
    public void ToStateMetadata_ReturnsOnlyTheTtlKey()
    {
        // Arrange
        var config = new IdempotencyConfig { RecordTtl = TimeSpan.FromDays(30) };

        // Act
        var metadata = config.ToStateMetadata();

        // Assert
        Assert.Equal(["ttlInSeconds"], metadata.Keys);
    }

    [Theory]
    [InlineData(0, 12, 0, 0, "43200")]
    [InlineData(0, 0, 30, 0, "1800")]
    [InlineData(1, 0, 0, 0, "86400")]
    [InlineData(365, 0, 0, 0, "31536000")]
    public void ToStateMetadata_WithConfiguredTtl_ConvertsToSeconds(int days, int hours, int minutes, int seconds,
        string expected)
    {
        // Arrange
        var config = new IdempotencyConfig { RecordTtl = new TimeSpan(days, hours, minutes, seconds) };

        // Act
        var metadata = config.ToStateMetadata();

        // Assert
        Assert.Equal(expected, metadata["ttlInSeconds"]);
    }

    [Fact]
    public void ToStateMetadata_WithSubSecondPrecision_TruncatesTowardsZero()
    {
        // Arrange
        var config = new IdempotencyConfig { RecordTtl = TimeSpan.FromMilliseconds(1500) };

        // Act
        var metadata = config.ToStateMetadata();

        // Assert
        Assert.Equal("1", metadata["ttlInSeconds"]);
    }

    [Fact]
    public void ToStateMetadata_WithVeryLargeTtl_DoesNotOverflow()
    {
        // Arrange
        // An int-based conversion would overflow past ~68 years of seconds.
        var config = new IdempotencyConfig { RecordTtl = TimeSpan.FromDays(365 * 100) };

        // Act
        var metadata = config.ToStateMetadata();

        // Assert
        Assert.Equal("3153600000", metadata["ttlInSeconds"]);
    }
}
