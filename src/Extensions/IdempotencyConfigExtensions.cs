using System.Globalization;
using Intropy.IdempotencyService.Configuration;

namespace Intropy.IdempotencyService.Extensions;

public static class IdempotencyConfigExtensions
{
    public static Dictionary<string, string> ToStateMetadata(this IdempotencyConfig config) =>
        new()
        {
            ["ttlInSeconds"] = ((long)config.RecordTtl.TotalSeconds).ToString(CultureInfo.InvariantCulture)
        };
}
