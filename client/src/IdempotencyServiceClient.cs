using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Intropy.Contracts.IdempotencyService;
using Microsoft.Extensions.Options;

namespace Intropy.IdempotencyService.Client;

/// <summary>
/// Client for interacting with the Intropy Idempotency Service using Dapr service invocation.
/// </summary>
public class IdempotencyServiceClient(DaprClient daprClient, IOptions<IdempotencyServiceClientOptions>? options)
    : IIdempotencyServiceClient
{
    private readonly DaprClient _daprClient = daprClient ?? throw new ArgumentNullException(nameof(daprClient));
    private readonly string _appId = options?.Value.AppId ?? throw new ArgumentNullException(nameof(options));

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc/>
    public async Task<StatusResponse> GetStatusAsync(MessageInfo messageInfo,
        CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(messageInfo);

        using var client = _daprClient.CreateInvokableHttpClient(_appId);
        var request = new HttpRequestMessage(HttpMethod.Post, "status");
        request.Content = JsonContent.Create(messageInfo, options: JsonOptions);

        try
        {
            var res = await client.SendAsync(request, cancellationToken);

            if (!res.IsSuccessStatusCode)
            {
                var details = await res.Content.ReadAsStringAsync(cancellationToken);
                throw new IdempotencyServiceException("Failed to get status", (int)res.StatusCode, details);
            }

            var obj = await JsonSerializer.DeserializeAsync<StatusResponse>(
                await res.Content.ReadAsStreamAsync(cancellationToken), JsonOptions, cancellationToken);

            if (obj is null)
                throw new IdempotencyServiceException("Failed to deserialize response", (int)res.StatusCode, null);
            return obj;
        }
        catch (Exception ex) when (ex.GetType().Name == "InvocationException")
        {
            throw new IdempotencyServiceException(
                "Failed to get status",
                500,
                ex.Message);
        }
        catch (Exception ex) when (ex.GetType().Name == "DaprException")
        {
            throw new IdempotencyServiceException("Dapr service invocation failed", ex);
        }
    }

    /// <inheritdoc/>
    public async Task CommitAsync(MessageInfo messageInfo,
        CancellationToken cancellationToken = new())
    {
        ArgumentNullException.ThrowIfNull(messageInfo);

        using var client = _daprClient.CreateInvokableHttpClient(_appId);
        var request = new HttpRequestMessage(HttpMethod.Post, "commit");
        request.Content = JsonContent.Create(messageInfo, options: JsonOptions);

        try
        {
            var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var problemDetails = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new IdempotencyServiceException(
                    $"Failed to commit: {response.StatusCode}",
                    (int)response.StatusCode,
                    problemDetails);
            }
        }
        catch (Exception ex) when (ex.GetType().Name == "InvocationException")
        {
            throw new IdempotencyServiceException(
                "Failed to commit",
                500,
                ex.Message);
        }
        catch (Exception ex) when (ex.GetType().Name == "DaprException")
        {
            throw new IdempotencyServiceException("Dapr service invocation failed", ex);
        }
    }


    public async Task<MessageInfo?> GetInfoAsync(string component, string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(component))
            throw new ArgumentException("Integration cannot be null or empty", nameof(component));
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("Id cannot be null or empty", nameof(id));

        using var client = _daprClient.CreateInvokableHttpClient(_appId);
        var request = new HttpRequestMessage(HttpMethod.Get, $"components/{component}/{id}");

        try
        {
            var response = await client.SendAsync(request, cancellationToken);

            if (response.StatusCode == HttpStatusCode.NoContent)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                var problemDetails = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new IdempotencyServiceException(
                    $"Failed to get info: {response.StatusCode}",
                    (int)response.StatusCode,
                    problemDetails);
            }

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<MessageInfo>(content, JsonOptions);
        }
        catch (Exception ex) when (ex.GetType().Name == "DaprException")
        {
            throw new IdempotencyServiceException("Dapr service invocation failed", ex);
        }
    }
}
