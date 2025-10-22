using System.Text.Json.Serialization;

namespace MailTmBlazor.Infrastructure.MailTm.ApiContracts;

public sealed class HydraPage<T>
{
    [JsonPropertyName("hydra:member")]
    public List<T> Items { get; set; } = [];

    [JsonPropertyName("hydra:totalItems")]
    public int TotalItems { get; set; }
}