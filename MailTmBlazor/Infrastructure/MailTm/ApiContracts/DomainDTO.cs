using System.Text.Json.Serialization;

namespace MailTmBlazor.Infrastructure.MailTm.ApiContracts;

public sealed class DomainDto
{
    [JsonPropertyName("id")]        public string Id { get; set; } = default!;
    [JsonPropertyName("domain")]    public string Domain { get; set; } = default!;
    [JsonPropertyName("isActive")]  public bool IsActive { get; set; }
    [JsonPropertyName("isPrivate")] public bool IsPrivate { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public DateTime UpdatedAt { get; set; }
}