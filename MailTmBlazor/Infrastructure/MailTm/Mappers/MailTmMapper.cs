using MailTmBlazor.Domain.Entities;
using MailTmBlazor.Infrastructure.MailTm.ApiContracts;

namespace MailTmBlazor.Infrastructure.MailTm.Mappers;

public static class MailTmMapper
{
    public static Domain.Entities.DomainName ToDomain(this DomainDto d) =>
        new(d.Id, d.Domain, d.IsActive, d.IsPrivate, d.CreatedAt, d.UpdatedAt);

    public static Account ToDomain(this AccountDto a) =>
        new(a.Id, a.Address, a.Quota, a.Used, a.IsDisabled, a.IsDeleted, a.CreatedAt, a.UpdatedAt);

    public static Message ToDomain(this MessageListItemDto m) =>
        new(m.Id, m.Subject, m.Seen, m.Intro, m.From?.Name ?? "", m.From.Address, m.CreatedAt, m.HasAttachments);

    public static Message ToDomain(this MessageDetailDto d) =>
        new(d.Id, d.Subject, d.Seen, "", d.From?.Name ?? "", d.From.Address, d.CreatedAt, d.HasAttachments,
            d.Text, d.Html,
            d.Attachments?.Select(a => new Attachment(a.Id, a.Filename, a.ContentType, a.DownloadUrl, a.Size)).ToList());
}