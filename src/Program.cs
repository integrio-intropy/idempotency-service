using System.Text.Json.Serialization;
using Intropy.IdempotencyService.Configuration;
using Intropy.IdempotencyService.Endpoints;
using Intropy.IdempotencyService.Services;
using Intropy.IdempotencyService.Telemetry;
using Intropy.Telemetry.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

builder.Services.AddOpenTelemetry(conf =>
{
    conf.ServiceNamespace = "intropy";
    conf.ServiceName = ActivitySourceProvider.ActivitySourceName;
    conf.Environment = environment;
});

builder.Services.AddOpenApi();
builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddDaprClient();
builder.Services.AddTransient<IIdempotencyService, IdempotencyService>();

builder.Services.AddOptions<DaprComponentsConfig>()
    .Bind(builder.Configuration.GetSection("DaprComponentsConfig"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<IdempotencyConfig>()
    .Bind(builder.Configuration.GetSection("IdempotencyConfig"))
    .Validate(c => c.RecordTtl > TimeSpan.Zero, "IdempotencyConfig:RecordTtl must be greater than zero.")
    .ValidateOnStart();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapEndpoints();

app.Run();
