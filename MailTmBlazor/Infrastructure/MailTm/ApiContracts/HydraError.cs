using System.Text.Json.Serialization;

namespace MailTmBlazor.Infrastructure.MailTm.ApiContracts;

public sealed class HydraError
{
    [JsonPropertyName("hydra:title")]
    public string? Title { get; set; }

    [JsonPropertyName("hydra:description")]
    public string? Description { get; set; }
}
