using System.Text.Json;
using System.Text.Json.Serialization;
using Dapr.Client;
using Intropy.Contracts.IdempotencyService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Intropy.IdempotencyService.Client;

/// <summary>
/// Extension methods for configuring the Idempotency Service client in dependency injection.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <param name="services">The service collection to add the client to.</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds the Idempotency Service client to the service collection with optional configuration.
        /// </summary>
        /// <param name="configureOptions">Optional action to configure the client options.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddIdempotencyServiceClient(
            Action<IdempotencyServiceClientOptions>? configureOptions = null)
        {
            services.Configure<IdempotencyServiceClientOptions>(options => { configureOptions?.Invoke(options); });

            services.AddSingleton(serviceProvider =>
            {
                var options = serviceProvider
                                  .GetService<Microsoft.Extensions.Options.IOptions<IdempotencyServiceClientOptions>>()
                                  ?.Value
                              ?? new IdempotencyServiceClientOptions();

                var daprClientBuilder = new DaprClientBuilder();

                if (!string.IsNullOrEmpty(options.DaprHttpEndpoint))
                {
                    daprClientBuilder.UseHttpEndpoint(options.DaprHttpEndpoint);
                }

                JsonSerializerOptions jsonSerializerOptions = new()
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    Converters = { new JsonStringEnumConverter() }
                };

                daprClientBuilder.UseJsonSerializationOptions(jsonSerializerOptions);

                return daprClientBuilder.Build();
            });

            services.AddSingleton<IIdempotencyServiceClient, IdempotencyServiceClient>();

            return services;
        }

        /// <summary>
        /// Adds the Idempotency Service client to the service collection using configuration from IConfiguration.
        /// </summary>
        /// <param name="configuration">The configuration containing IdempotencyService settings.</param>
        /// <returns>The service collection for chaining.</returns>
        public IServiceCollection AddIdempotencyServiceClient(IConfiguration configuration)
        {
            services.Configure<IdempotencyServiceClientOptions>(configuration.GetSection("IdempotencyService"));
            return services.AddIdempotencyServiceClient();
        }
    }
}
