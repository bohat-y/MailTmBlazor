using System.Text.Json.Serialization;

namespace MailTmBlazor.Infrastructure.MailTm.ApiContracts;

public sealed class AccountDto
{
    [JsonPropertyName("id")]        public string Id { get; set; } = default!;
    [JsonPropertyName("address")]   public string Address { get; set; } = default!;
    [JsonPropertyName("quota")]     public long Quota { get; set; }
    [JsonPropertyName("used")]      public long Used { get; set; }
    [JsonPropertyName("isDisabled")]public bool IsDisabled { get; set; }
    [JsonPropertyName("isDeleted")] public bool IsDeleted { get; set; }
    [JsonPropertyName("createdAt")] public DateTime CreatedAt { get; set; }
    [JsonPropertyName("updatedAt")] public DateTime UpdatedAt { get; set; }
}