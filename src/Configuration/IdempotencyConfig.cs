namespace Intropy.IdempotencyService.Configuration;

public class IdempotencyConfig
{
    /// <summary>
    /// How long a committed record is retained in the state store before it expires.
    /// Defaults to 30 days.
    /// </summary>
    public TimeSpan RecordTtl { get; set; } = TimeSpan.FromDays(30);
}
