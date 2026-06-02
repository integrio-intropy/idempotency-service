using System.ComponentModel.DataAnnotations;

namespace Intropy.IdempotencyService.Configuration;

public class DaprComponentsConfig
{
    [Required] public required string StateStoreName { get; set; }
}
