namespace Intropy.IdempotencyService.Models;

public record Record(string Hash, DateTimeOffset Timestamp);
