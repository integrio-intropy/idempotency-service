using CSharpFunctionalExtensions;
using Intropy.Contracts.IdempotencyService;
using Intropy.IdempotencyService.Extensions;
using Action = Intropy.Contracts.IdempotencyService.Action;
using Record = Intropy.IdempotencyService.Models.Record;

namespace Intropy.IdempotencyService.Test;

public class RecordExtensionsTests
{
    [Fact]
    public void ToStatusResponse_WithNoPreviousData_ShouldProceed()
    {
        // Arrange
        const string hash = "f27597a74d6d81b58e42b4fc9be85d6861eeeee4dccc13697a1ed5148470097d";
        var timestamp = new DateTimeOffset(2025, 6, 22, 10, 13, 11, TimeSpan.Zero);

        var messageInfo = new MessageInfo("int101", "1337", hash, timestamp);
        var maybeRecord = Maybe<Record>.None;

        // Act
        var result = maybeRecord.ToStatusResponse(messageInfo);

        //Assert
        Assert.Equal(Action.Proceed, result.Action);
        Assert.Equal(Reason.NoPreviousData, result.Reason);
    }

    [Fact]
    public void ToStatusResponse_WithSameDataDifferentTimestamp_ShouldIgnore()
    {
        // Arrange
        const string hash = "f27597a74d6d81b58e42b4fc9be85d6861eeeee4dccc13697a1ed5148470097d";
        var timestampCurrent = new DateTimeOffset(2025, 6, 22, 10, 13, 11, TimeSpan.Zero);
        var timestampStored = new DateTimeOffset(2025, 6, 21, 22, 55, 11, TimeSpan.Zero);

        var messageInfo = new MessageInfo("int101", "1337", hash, timestampCurrent);
        var maybeRecord = Maybe<Record>.From(new Record(hash, timestampStored));

        // Act
        var result = maybeRecord.ToStatusResponse(messageInfo);

        //Assert
        Assert.Equal(Action.Ignore, result.Action);
        Assert.Equal(Reason.SameData, result.Reason);
    }

    [Fact]
    public void ToStatusResponse_WithDifferentDataOlder_ShouldIgnore()
    {
        // Arrange
        const string hash1 = "f27597a74d6d81b58e42b4fc9be85d6861eeeee4dccc13697a1ed5148470097d";
        const string hash2 = "861eeeee4dccc13697a1ed5148470097df27597a74d6d81b58e42b4fc9be85d6";
        var timestampCurrent = new DateTimeOffset(2025, 6, 22, 10, 2, 11, TimeSpan.Zero);
        var timestampStored = new DateTimeOffset(2025, 6, 22, 10, 13, 11, TimeSpan.Zero);

        var messageInfo = new MessageInfo("int101", "1337", hash1, timestampCurrent);
        var maybeRecord = Maybe<Record>.From(new Record(hash2, timestampStored));

        // Act
        var result = maybeRecord.ToStatusResponse(messageInfo);

        //Assert
        Assert.Equal(Action.Ignore, result.Action);
        Assert.Equal(Reason.StaleData, result.Reason);
    }

    [Fact]
    public void ToStatusResponse_WithDifferentDataNewer_ShouldProceed()
    {
        // Arrange
        const string hash1 = "f27597a74d6d81b58e42b4fc9be85d6861eeeee4dccc13697a1ed5148470097d";
        const string hash2 = "861eeeee4dccc13697a1ed5148470097df27597a74d6d81b58e42b4fc9be85d6";
        var timestampCurrent = new DateTimeOffset(2025, 6, 22, 10, 55, 11, TimeSpan.Zero);
        var timestampStored = new DateTimeOffset(2025, 6, 22, 10, 13, 11, TimeSpan.Zero);

        var messageInfo = new MessageInfo("int101", "1337", hash1, timestampCurrent);
        var maybeRecord = Maybe<Record>.From(new Record(hash2, timestampStored));

        // Act
        var result = maybeRecord.ToStatusResponse(messageInfo);

        //Assert
        Assert.Equal(Action.Proceed, result.Action);
        Assert.Equal(Reason.NewerData, result.Reason);
    }

    [Fact]
    public void ToStatusResponse_WithDifferentDataSameTime_ShouldIgnore()
    {
        // Arrange
        const string hash1 = "f27597a74d6d81b58e42b4fc9be85d6861eeeee4dccc13697a1ed5148470097d";
        const string hash2 = "861eeeee4dccc13697a1ed5148470097df27597a74d6d81b58e42b4fc9be85d6";
        var timestampCurrent = new DateTimeOffset(2025, 6, 22, 10, 55, 11, TimeSpan.Zero);
        var timestampStored = new DateTimeOffset(2025, 6, 22, 10, 55, 11, TimeSpan.Zero);

        var messageInfo = new MessageInfo("int101", "1337", hash1, timestampCurrent);
        var maybeRecord = Maybe<Record>.From(new Record(hash2, timestampStored));

        // Act
        var result = maybeRecord.ToStatusResponse(messageInfo);

        //Assert
        Assert.Equal(Action.Ignore, result.Action);
        Assert.Equal(Reason.StaleData, result.Reason);
    }

    [Fact]
    public void ToStatusResponse_WithDifferentTimeZones_SameInstant_ReturnsStaleData()
    {
        // Arrange
        // Both represent 10:00 UTC
        var storedTimestamp = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var incomingTimestamp = new DateTimeOffset(2024, 1, 15, 11, 0, 0, TimeSpan.FromHours(1));

        var maybeRecord = Maybe<Record>.From(new Record("hash123", storedTimestamp));
        var messageInfo = new MessageInfo("int101", "1337", "hash456", incomingTimestamp);

        // Act
        var result = maybeRecord.ToStatusResponse(messageInfo);

        // Assert
        Assert.Equal(Action.Ignore, result.Action);
        Assert.Equal(Reason.StaleData, result.Reason);
    }

    [Fact]
    public void ToStatusResponse_WithDifferentTimeZones_IncomingNewer_ComparesCorrectly()
    {
        // Arrange
        // Stored: 10:00 UTC
        // Incoming: 12:00 CET = 11:00 UTC (newer)
        var storedTimestamp = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.Zero);
        var incomingTimestamp = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.FromHours(1));

        var maybeRecord = Maybe<Record>.From(new Record("hash123", storedTimestamp));
        var messageInfo = new MessageInfo("int101", "1337", "hash456", incomingTimestamp);

        // Act
        var result = maybeRecord.ToStatusResponse(messageInfo);

        // Assert
        Assert.Equal(Action.Proceed, result.Action);
        Assert.Equal(Reason.NewerData, result.Reason);
    }

    [Fact]
    public void ToStatusResponse_WithDifferentTimeZones_IncomingOlder_ComparesCorrectly()
    {
        // Arrange
        // Stored: 12:00 CET = 11:00 UTC
        // Incoming: 10:00 UTC (older)
        var storedTimestamp = new DateTimeOffset(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);
        var incomingTimestamp = new DateTimeOffset(2024, 1, 15, 10, 0, 0, TimeSpan.FromHours(1));

        var maybeRecord = Maybe<Record>.From(new Record("hash123", storedTimestamp));
        var messageInfo = new MessageInfo("int101", "1337", "hash456", incomingTimestamp);

        // Act
        var result = maybeRecord.ToStatusResponse(messageInfo);

        // Assert
        Assert.Equal(Action.Ignore, result.Action);
        Assert.Equal(Reason.StaleData, result.Reason);
    }
}
