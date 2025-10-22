using MailTmBlazor.Application.Abstractions;
using MailTmBlazor.Domain.Entities;
using MailTmBlazor.Infrastructure.MailTm.ApiContracts;
using MailTmBlazor.Infrastructure.MailTm.Mappers;
using MailTmBlazor.Infrastructure.Auth;

namespace MailTmBlazor.Infrastructure.MailTm;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public sealed class MailTmHttpClient : IAuthService, IMailboxService
{
    private readonly IAccountSession _session;
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public MailTmHttpClient(IAccountSession session, HttpClient http)
    {
        _session = session;
        _http = http;
    }

    // --- Auth ---
    
    

    public async Task<Account> RegisterAsync(string address, string password)
    {
        var res = await _http.PostAsJsonAsync("accounts", new { address, password }, Json);

        if (res.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            // account already exists → pretend success so the flow can continue to LoginAsync
            return new Account(id: "", address, quota: 0, used: 0, isDisabled: false, isDeleted: false,
                createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);
        }

        res.EnsureSuccessStatusCode();
        var dto = await res.Content.ReadFromJsonAsync<AccountDto>(Json) ?? throw new("No account payload");
        return dto.ToDomain();
    }
    

    public async Task LoginAsync(string address, string password)
    {
        // 1) token
        var tokRes = await _http.PostAsJsonAsync("token", new { address, password }, Json);
        tokRes.EnsureSuccessStatusCode();
        var tokenDto = await tokRes.Content.ReadFromJsonAsync<TokenDto>(Json) ?? throw new("Auth failed");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDto.Token);

        // 2) me
        var me = await _http.GetFromJsonAsync<AccountDto>("me", Json) ?? throw new("No /me data");

        // 3) persist combined snapshot
        await _session.SetAsync(new AccountSnapshot(
            tokenDto.Token, me.Id, me.Address, me.Quota, me.Used, me.IsDisabled, me.IsDeleted, me.CreatedAt, me.UpdatedAt
        ));
    }

    public async Task<Account> MeAsync()
    {
        await EnsureAuthAsync();
        var dto = await _http.GetFromJsonAsync<AccountDto>("me", Json) ?? throw new("No /me data");
        return dto.ToDomain();
    }

    public async Task LogoutAsync()
    {
        _http.DefaultRequestHeaders.Authorization = null;
        await _session.ClearAsync();
    }

    // --- Mailbox ---

    public async Task<IReadOnlyList<DomainName>> GetDomainsAsync(int page = 1)
    {
        var hydra = await _http.GetFromJsonAsync<HydraPage<DomainDto>>($"domains?page={page}", Json);
        return (hydra?.Items ?? []).Select(d => d.ToDomain()).ToList();
    }

    public async Task<PagedResult<Message>> GetMessagesAsync(int page = 1)
    {
        await EnsureAuthAsync();
        var hydra = await _http.GetFromJsonAsync<HydraPage<MessageListItemDto>>($"messages?page={page}", Json);
        var items = (hydra?.Items ?? []).Select(m => m.ToDomain()).ToList();
        return new(items, hydra?.TotalItems ?? items.Count);
    }

    public async Task<Message> GetMessageAsync(string id)
    {
        await EnsureAuthAsync();
        var dto = await _http.GetFromJsonAsync<MessageDetailDto>($"messages/{id}", Json) ?? throw new("Not found");
        return dto.ToDomain();
    }

    public async Task MarkSeenAsync(string id, bool seen = true)
    {
        await EnsureAuthAsync();
        var req = new HttpRequestMessage(HttpMethod.Patch, $"messages/{id}") { Content = JsonContent.Create(new { seen }) };
        var res = await _http.SendAsync(req);
        res.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(string id)
    {
        await EnsureAuthAsync();
        var res = await _http.DeleteAsync($"messages/{id}");
        res.EnsureSuccessStatusCode();
    }

    // --- helper ---
    private async Task EnsureAuthAsync()
    {
        if (_http.DefaultRequestHeaders.Authorization is not null) return;

        var snap = await _session.GetAsync();
        if (snap is not null)
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", snap.Token);
        else
            throw new InvalidOperationException("Not authenticated");
    }
}