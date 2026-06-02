using CSharpFunctionalExtensions;
using Intropy.Contracts.IdempotencyService;
using Intropy.IdempotencyService.Models;
using Action = Intropy.Contracts.IdempotencyService.Action;

namespace Intropy.IdempotencyService.Extensions;

public static class RecordExtensions
{
    public static StatusResponse ToStatusResponse(this Maybe<Record> maybeRecord, MessageInfo messageInfo)
    {
        if (maybeRecord.HasNoValue)
            return new StatusResponse(Action.Proceed, Reason.NoPreviousData);

        if (IsSameData(messageInfo.Hash, maybeRecord.Value.Hash))
            return new StatusResponse(Action.Ignore, Reason.SameData);

        if (IsStaleData(messageInfo.Timestamp, maybeRecord.Value.Timestamp))
            return new StatusResponse(Action.Ignore, Reason.StaleData);

        return new StatusResponse(Action.Proceed, Reason.NewerData);
    }

    private static bool IsSameData(string value1, string value2) => value1.Equals(value2, StringComparison.Ordinal);

    private static bool IsStaleData(DateTimeOffset currentTimestamp, DateTimeOffset storedTimestamp) =>
        currentTimestamp <= storedTimestamp;
}
