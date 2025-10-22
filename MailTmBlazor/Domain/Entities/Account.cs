namespace MailTmBlazor.Domain.Entities;

public sealed class Account(
    string id, string address, long quota, long used,
    bool isDisabled, bool isDeleted, DateTime createdAt, DateTime updatedAt)
{
    public string Id { get; } = id;
    public string Address { get; } = address;
    public long Quota { get; } = quota;
    public long Used { get; } = used;
    public bool IsDisabled { get; } = isDisabled;
    public bool IsDeleted { get; } = isDeleted;
    public DateTime CreatedAt { get; } = createdAt;
    public DateTime UpdatedAt { get; } = updatedAt;
}