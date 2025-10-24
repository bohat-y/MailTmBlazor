namespace MailTmBlazor.Domain.Entities;

public sealed class DomainName(
    string id, string domain, bool isActive, bool isPrivate,
    DateTime createdAt, DateTime updatedAt)
{
    public string Id { get; } = id;
    public string Domain { get; } = domain;
    public bool IsActive { get; } = isActive;
    public bool IsPrivate { get; } = isPrivate;
    public DateTime CreatedAt { get; } = createdAt;
    public DateTime UpdatedAt { get; } = updatedAt;
}