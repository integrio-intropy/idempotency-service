using CSharpFunctionalExtensions;
using Intropy.Contracts.IdempotencyService;
using Microsoft.AspNetCore.Mvc;

namespace Intropy.IdempotencyService.Services;

public interface IIdempotencyService
{
    Task<Result<Maybe<MessageInfo>, ProblemDetails>> GetInfo(string component, string id,
        CancellationToken? token = null);

    Task<Result<StatusResponse, ProblemDetails>> GetStatus(MessageInfo messageInfo,
        CancellationToken? token = null);

    Task<UnitResult<ProblemDetails>> Commit(MessageInfo messageInfo, CancellationToken? token = null);
}
