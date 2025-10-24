using System.Text.Json.Serialization;

namespace MailTmBlazor.Infrastructure.MailTm.ApiContracts;

public sealed class AddressDto
{
    [JsonPropertyName("name")]    public string? Name { get; set; }
    [JsonPropertyName("address")] public string Address { get; set; } = default!;
}

public sealed class MessageListItemDto
{
    [JsonPropertyName("id")]            public string Id { get; set; } = default!;
    [JsonPropertyName("from")]          public AddressDto From { get; set; } = default!;
    [JsonPropertyName("subject")]       public string Subject { get; set; } = default!;
    [JsonPropertyName("intro")]         public string Intro { get; set; } = default!;
    [JsonPropertyName("seen")]          public bool Seen { get; set; }
    [JsonPropertyName("hasAttachments")]public bool HasAttachments { get; set; }
    [JsonPropertyName("downloadUrl")]   public string DownloadUrl { get; set; } = default!;
    [JsonPropertyName("createdAt")]     public DateTime CreatedAt { get; set; }
}

public sealed class AttachmentDto
{
    [JsonPropertyName("id")]          public string Id { get; set; } = default!;
    [JsonPropertyName("filename")]    public string Filename { get; set; } = default!;
    [JsonPropertyName("contentType")] public string ContentType { get; set; } = default!;
    [JsonPropertyName("downloadUrl")] public string DownloadUrl { get; set; } = default!;
    [JsonPropertyName("size")]        public int Size { get; set; }
}

public sealed class MessageDetailDto
{
    [JsonPropertyName("id")]            public string Id { get; set; } = default!;
    [JsonPropertyName("from")]          public AddressDto From { get; set; } = default!;
    [JsonPropertyName("to")]            public List<AddressDto> To { get; set; } = [];
    [JsonPropertyName("subject")]       public string Subject { get; set; } = default!;
    [JsonPropertyName("seen")]          public bool Seen { get; set; }
    [JsonPropertyName("text")]          public string? Text { get; set; }
    [JsonPropertyName("html")]          public List<string>? Html { get; set; }
    [JsonPropertyName("hasAttachments")]public bool HasAttachments { get; set; }
    [JsonPropertyName("attachments")]   public List<AttachmentDto>? Attachments { get; set; }
    [JsonPropertyName("downloadUrl")]   public string DownloadUrl { get; set; } = default!;
    [JsonPropertyName("createdAt")]     public DateTime CreatedAt { get; set; }
}