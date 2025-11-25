using MailTmBlazor.Domain.Entities;

namespace MailTmBlazor.Application.Abstractions;

public interface IAuthService
{
    Task<Account> RegisterAsync(string address, string password);
    Task LoginAsync(string address, string password);
    Task<Account> LoginAndFetchAsync(string address, string password);
    Task<Account> MeAsync();
    Task LogoutAsync();
}
