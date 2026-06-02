# Intropy Idempotency Service Client

A .NET client library for the Intropy Idempotency Service that uses Dapr service invocation for distributed message deduplication and idempotency checking.

## Installation

```bash
dotnet add package Intropy.IdempotencyService.Client
```
## Usage

### Configuration with Dependency Injection

```csharp
using Intropy.IdempotencyService.Client;

// In your Startup.cs or Program.cs
services.AddIdempotencyServiceClient(options =>
{
    options.AppId = "idempotency-service"; // Dapr app ID of the target service
    options.DaprHttpEndpoint = "http://localhost:3500"; // Optional: Local Dapr sidecar HTTP endpoint (default: http://localhost:3500)
});
```

Or configure using appsettings.json:

```json
{
  "IdempotencyService": {
    "AppId": "idempotency-service",
    "DaprHttpEndpoint": "http://localhost:3500"
  }
}
```

```csharp
services.AddIdempotencyServiceClient(Configuration);
```

### Using the Client


```csharp
using Intropy.IdempotencyService.Client;
using Intropy.IdempotencyService.Client.Models;

public class MyService
{
    private readonly IIdempotencyServiceClient _idempotencyClient;

    public MyService(IIdempotencyServiceClient idempotencyClient)
    {
        _idempotencyClient = idempotencyClient;
    }

    public async Task ProcessMessage(string component, string messageId, string messageContent)
    {
        // Check if message was already processed
        var existingInfo = await _idempotencyClient.GetInfoAsync(component, messageId, CancellationToken.None);

        if (existingInfo != null)
        {
            // Message was already processed
            return;
        }

        // Create message info with hash of content
        var messageInfo = new MessageInfo(
            Component: component,
            Id: messageId,
            Hash: ComputeHash(messageContent),
            Timestamp: DateTime.UtcNow
        );

        // Check if we should process this message
        var status = await _idempotencyClient.GetStatusAsync(messageInfo);

        if (status.Action == Action.Proceed)
        {
            // Process the message
            await ProcessMessageInternal(messageContent);

            // Commit the message to mark it as processed
            await _idempotencyClient.CommitAsync(messageInfo);
        }
        else
        {
            // Message should be ignored
            Console.WriteLine($"Ignoring message: {status.Reason}");
        }
    }

    private string ComputeHash(string content)
    {
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(content);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
```

## API Reference

### IIdempotencyServiceClient

- `GetInfoAsync(string component, string id, CancellationToken cancellationToken)` - Retrieve message info if it exists
- `GetStatusAsync(MessageInfo messageInfo)` - Check if a message should be processed or ignored
- `CommitAsync(MessageInfo messageInfo)` - Commit a message record to mark it as processed

### Models

- `MessageInfo` - Contains component name, message ID, content hash, and timestamp
- `StatusResponse` - Contains action (Proceed/Ignore) and reason
- `Action` - Enum: Proceed or Ignore
- `Reason` - Enum: NewerData, SameData, StaleData, or NoPreviousData

## Requirements

- .NET 10.0 or later
- Dapr runtime with sidecar configured
- Access to the Idempotency Service via Dapr service invocation

## Links

- [Source repository](https://github.com/integrio-intropy/idempotency-service)
- [Report an issue](https://github.com/integrio-intropy/idempotency-service/issues)

## License

[MIT](https://github.com/integrio-intropy/idempotency-service/blob/main/LICENSE)
