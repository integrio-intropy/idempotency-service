using Intropy.Contracts.IdempotencyService;
using Intropy.IdempotencyService.Services;
using Microsoft.AspNetCore.Mvc;

namespace Intropy.IdempotencyService.Endpoints;

public static class Endpoints
{
    public static void MapEndpoints(this WebApplication app)
    {
        app.MapGet("/info/{component}/{id}",
                async (string component, string id, CancellationToken token,
                    [FromServices] IIdempotencyService service) =>
                {
                    var result = await service.GetInfo(component, id, token);
                    if (result.IsFailure)
                        return Results.Problem(result.Error);

                    if (result.Value.HasNoValue)
                        return Results.NoContent();

                    return Results.Ok(result.Value.Value);
                })
            .WithName("info")
            .WithDisplayName("Info")
            .WithSummary("Info")
            .WithDescription("See details about a message")
            .Produces<MessageInfo>()
            .Produces(204)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(500);

        app.MapPost("/status",
                async ([FromBody] MessageInfo messageInfo, CancellationToken token,
                    [FromServices] IIdempotencyService service) =>
                {
                    var result = await service.GetStatus(messageInfo, token);
                    if (result.IsFailure)
                        return Results.Problem(result.Error);

                    return Results.Ok(result.Value);
                })
            .WithName("status")
            .WithDisplayName("Status")
            .WithSummary("Status")
            .WithDescription("Check status for a message")
            .Produces<StatusResponse>()
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(500);

        app.MapPost("/commit", async ([FromBody] MessageInfo messageInfo, CancellationToken token,
                [FromServices] IIdempotencyService service) =>
            {
                var result = await service.Commit(messageInfo, token);
                return result.IsFailure
                    ? Results.Problem(result.Error)
                    : Results.Ok();
            })
            .WithName("commit")
            .WithDisplayName("Commit")
            .WithSummary("Commit")
            .WithDescription("Commit state for a message")
            .Produces(200)
            .Produces<ProblemDetails>(400)
            .Produces<ProblemDetails>(409)
            .Produces<ProblemDetails>(500);

        app.MapGet("/healthz", () => Task.FromResult(Results.Ok()));
    }
}
