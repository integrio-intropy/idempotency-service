using CSharpFunctionalExtensions;
using Dapr.Client;
using Intropy.Contracts.IdempotencyService;
using Intropy.IdempotencyService.Extensions;
using Intropy.IdempotencyService.Configuration;
using Intropy.IdempotencyService.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Action = Intropy.Contracts.IdempotencyService.Action;

namespace Intropy.IdempotencyService.Services;

public class IdempotencyService(
    ILogger<IdempotencyService> logger,
    DaprClient dapr,
    IOptions<DaprComponentsConfig> daprConfig,
    IOptions<IdempotencyConfig> idempotencyConfig) : IIdempotencyService
{
    public async Task<Result<Maybe<MessageInfo>, ProblemDetails>> GetInfo(string component, string id,
        CancellationToken? token = null)
    {
        var recordResult = await GetRecord(component, id, token);
        if (recordResult.IsFailure)
            return recordResult.Error;

        if (recordResult.Value.HasNoValue)
            return Maybe<MessageInfo>.None;

        var messageInfo = new MessageInfo(
            Component: component,
            Id: id,
            Hash: recordResult.Value.Value.Hash,
            Timestamp: recordResult.Value.Value.Timestamp);
        return Maybe.From(messageInfo);
    }

    public async Task<Result<StatusResponse, ProblemDetails>> GetStatus(MessageInfo messageInfo,
        CancellationToken? token = null)
    {
        var recordResult = await GetRecord(messageInfo.Component, messageInfo.Id, token);
        if (recordResult.IsFailure)
            return recordResult.Error;

        return recordResult.Value.ToStatusResponse(messageInfo);
    }

    public async Task<UnitResult<ProblemDetails>> Commit(MessageInfo messageInfo, CancellationToken? token = null)
    {
        var record = new Record(messageInfo.Hash, messageInfo.Timestamp);

        var recordResult = await GetStatus(messageInfo, token);
        if (recordResult.IsFailure)
            return recordResult.Error;
        if (recordResult.Value.Action == Action.Ignore)
        {
            return new ProblemDetails
            {
                Status = 409,
                Title = "Attempted to commit a record that is older than the current or has the same data",
                Detail = $"Reason: {recordResult.Value.Reason.ToString()}"
            };
        }

        try
        {
            await dapr.SaveStateAsync(
                daprConfig.Value.StateStoreName,
                GetStateStoreKey(messageInfo.Component, messageInfo.Id),
                record,
                null,
                idempotencyConfig.Value.ToStateMetadata(),
                token ?? CancellationToken.None);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error saving record");
            return new ProblemDetails
            {
                Status = 500,
                Title = "An error occurred while saving record",
                Detail = e.Message
            };
        }

        return UnitResult.Success<ProblemDetails>();
    }

    private async Task<Result<Maybe<Record>, ProblemDetails>> GetRecord(string component, string id,
        CancellationToken? token = null)
    {
        try
        {
            var record = await dapr.GetStateAsync<Record>(daprConfig.Value.StateStoreName,
                GetStateStoreKey(component, id), null, null, token ?? CancellationToken.None);

            return Maybe.From(record);
        }
        catch (Exception e)
        {
            logger.LogError(e, "failed to get message info");
            return new ProblemDetails
            {
                Status = 500,
                Title = "An error occurred while retrieving record",
                Detail = e.Message
            };
        }
    }

    private static string GetStateStoreKey(string component, string id) => $"{component}|{id}";
}
