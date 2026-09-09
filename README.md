# Intropy Idempotency Service

The Idempotency Service provides distributed message idempotency checking for integration components within the Intropy ecosystem. It ensures that messages are processed exactly once, while also handling message updates based on timestamps.

## How it works

A component processes a message and asks the service whether to **proceed** or **ignore**, supplying:

- **Component** — which integration is processing the message
- **Id** — unique identifier for the message
- **Hash** — SHA-256 hash of the message content
- **Timestamp** — when the message was created/updated

Decision logic:

| Stored state | Hash matches | Newer timestamp | Result |
|---|---|---|---|
| No record | — | — | **Proceed** (NoPreviousData) |
| Record exists | yes | — | **Ignore** (SameData) |
| Record exists | no | yes | **Proceed** (NewerData) |
| Record exists | no | no | **Ignore** (StaleData) |

## Configuration

| Setting | Environment variable | Default | Description |
|---|---|---|---|
| `DaprComponentsConfig:StateStoreName` | `DaprComponentsConfig__StateStoreName` | — (required) | Name of the Dapr state store component used to persist records. |
| `IdempotencyConfig:RecordTtl` | `IdempotencyConfig__RecordTtl` | `30.00:00:00` (30 days) | How long a committed record is retained before it expires. |

`RecordTtl` is a .NET `TimeSpan` in `[d.]hh:mm:ss` form — `30.00:00:00` is 30 days, `12:00:00` is 12 hours. It must be greater than zero; the service fails to start otherwise.

The TTL is stamped onto each record when it is committed, so changing `RecordTtl` only affects records committed afterwards — it does not re-age records already in the state store.

Via the Helm chart:

```yaml
idempotency:
  recordTtl: "30.00:00:00"
```

## Repository layout

- `src/` — the ASP.NET Core service
- `client/src/` — the `Intropy.IdempotencyService.Client` NuGet package
- `client/test/`, `test/` — unit tests
- `charts/idempotency-service/` — Helm chart
- `api/openapi.yaml` — OpenAPI spec
- `local/` — `docker compose` setup for local development
- `docs/` — additional documentation

## Running locally

```bash
docker compose -f local/compose.yaml up
```

## Client library

Install the NuGet package:

```bash
dotnet add package Intropy.IdempotencyService.Client
```

Register it:

```csharp
builder.Services.AddIdempotencyServiceClient(options =>
{
    options.AppId = "idempotency-service";
});
```

Use it:

```csharp
var info = new MessageInfo(
    Component: "my-component",
    Id: messageId,
    Hash: ComputeSha256(content),
    Timestamp: DateTimeOffset.UtcNow);

var status = await idempotencyClient.GetStatusAsync(info);
if (status.Action == Action.Proceed)
{
    // process...
    await idempotencyClient.CommitAsync(info);
}
```

See [`client/src/README.md`](client/src/README.md) for the full client API reference.

## License

[MIT](LICENSE)
