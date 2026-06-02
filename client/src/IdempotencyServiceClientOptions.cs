namespace Intropy.IdempotencyService.Client;

/// <summary>
/// Configuration options for the Idempotency Service client.
/// </summary>
public class IdempotencyServiceClientOptions
{
    /// <summary>
    /// Gets or sets the Dapr app ID of the target idempotency service.
    /// Default value is "idempotency-service".
    /// </summary>
    public string AppId { get; set; } = "idempotency-service";

    /// <summary>
    /// Gets or sets the HTTP endpoint of the local Dapr sidecar.
    /// If not specified, defaults to http://localhost:3500.
    /// </summary>
    public string? DaprHttpEndpoint { get; set; }
}
