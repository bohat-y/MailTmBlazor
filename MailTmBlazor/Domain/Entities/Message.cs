namespace MailTmBlazor.Domain.Entities;

public sealed class Message(
    string id, string subject, bool seen, string intro,
    string fromName, string fromAddress, DateTime createdAt, bool hasAttachments,
    string? text = null, IReadOnlyList<string>? html = null, IReadOnlyList<Attachment>? attachments = null)
{
    public string Id { get; } = id;
    public string Subject { get; } = subject;
    public bool Seen { get; } = seen;
    public string Intro { get; } = intro;
    public string FromName { get; } = fromName;
    public string FromAddress { get; } = fromAddress;
    public DateTime CreatedAt { get; } = createdAt;
    public bool HasAttachments { get; } = hasAttachments;
    public string? Text { get; } = text;
    public IReadOnlyList<string>? Html { get; } = html;
    public IReadOnlyList<Attachment>? Attachments { get; } = attachments;
}

public sealed class Attachment(string id, string filename, string contentType, string downloadUrl, int size)
{
    public string Id { get; } = id;
    public string Filename { get; } = filename;
    public string ContentType { get; } = contentType;
    public string DownloadUrl { get; } = downloadUrl;
    public int Size { get; } = size;
}