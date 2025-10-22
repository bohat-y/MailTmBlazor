namespace MailTmBlazor.Infrastructure.Auth;

public interface IAccountSession
{
    Task<AccountSnapshot?> GetAsync();
    Task SetAsync(AccountSnapshot snapshot);
    Task ClearAsync();
}

public sealed record AccountSnapshot(
    string Token,
    string Id,
    string Address,
    long Quota,
    long Used,
    bool IsDisabled,
    bool IsDeleted,
    DateTime CreatedAt,
    DateTime UpdatedAt
);