using MailTmBlazor.Domain.Entities;

namespace MailTmBlazor.Application.Abstractions;

public interface IMailboxService
{
    Task<IReadOnlyList<DomainName>> GetDomainsAsync(int page = 1);
    Task<PagedResult<Message>> GetMessagesAsync(int page = 1);
    Task<Message> GetMessageAsync(string id);
    Task MarkSeenAsync(string id, bool seen = true);
    Task DeleteAsync(string id);
}

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total);