using System.Text.Json.Serialization;

namespace MailTmBlazor.Infrastructure.MailTm.ApiContracts;

public sealed class TokenDto
{
    [JsonPropertyName("id")]    public string Id { get; set; } = default!;
    [JsonPropertyName("token")] public string Token { get; set; } = default!;
}