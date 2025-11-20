using MailTmBlazor.Infrastructure.MailTm.ApiContracts;
using MailTmBlazor.Infrastructure.MailTm.Mappers;

namespace MailTmBlazor.Tests.Infrastructure.MailTm;

public class MailTmMapperTests
{
    [Fact]
    public void Domain_mapping_copies_all_fields()
    {
        var dto = new DomainDto
        {
            Id = "dom-1",
            Domain = "example.com",
            IsActive = true,
            IsPrivate = false,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };

        var domain = dto.ToDomain();

        Assert.Equal(dto.Id, domain.Id);
        Assert.Equal(dto.Domain, domain.Domain);
        Assert.Equal(dto.IsActive, domain.IsActive);
        Assert.Equal(dto.IsPrivate, domain.IsPrivate);
        Assert.Equal(dto.CreatedAt, domain.CreatedAt);
        Assert.Equal(dto.UpdatedAt, domain.UpdatedAt);
    }

    [Fact]
    public void Message_list_mapping_handles_missing_from_address()
    {
        var dto = new MessageListItemDto
        {
            Id = "msg-1",
            Subject = "Hello",
            Intro = "Intro text",
            Seen = false,
            HasAttachments = false,
            CreatedAt = DateTime.UtcNow,
            From = null!
        };

        var message = dto.ToDomain();

        Assert.Equal("msg-1", message.Id);
        Assert.Equal(string.Empty, message.FromName);
        Assert.Equal(string.Empty, message.FromAddress);
        Assert.Equal("Intro text", message.Intro);
    }

    [Fact]
    public void Message_detail_mapping_populates_attachments_and_html()
    {
        var created = DateTime.UtcNow;
        var dto = new MessageDetailDto
        {
            Id = "msg-2",
            Subject = "Subject",
            Seen = true,
            From = new AddressDto { Name = "Sender", Address = "sender@example.com" },
            Text = "plain",
            Html = new List<string> { "<p>plain</p>" },
            HasAttachments = true,
            CreatedAt = created,
            Attachments =
            [
                new AttachmentDto { Id = "a1", Filename = "file.txt", ContentType = "text/plain", DownloadUrl = "url", Size = 42 }
            ]
        };

        var message = dto.ToDomain();

        Assert.Equal(dto.Id, message.Id);
        Assert.Equal(dto.Subject, message.Subject);
        Assert.Equal("Sender", message.FromName);
        Assert.Equal("sender@example.com", message.FromAddress);
        Assert.Equal(dto.Text, message.Text);
        Assert.Equal(dto.Html, message.Html);
        Assert.True(message.HasAttachments);

        var attachment = Assert.Single(message.Attachments!);
        Assert.Equal("a1", attachment.Id);
        Assert.Equal("file.txt", attachment.Filename);
        Assert.Equal("text/plain", attachment.ContentType);
        Assert.Equal("url", attachment.DownloadUrl);
        Assert.Equal(42, attachment.Size);
    }
}
