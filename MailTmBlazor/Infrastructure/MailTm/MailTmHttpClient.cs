using MailTmBlazor.Application.Abstractions;
using MailTmBlazor.Domain.Entities;
using MailTmBlazor.Infrastructure.Auth;
using MailTmBlazor.Infrastructure.MailTm.ApiContracts;
using MailTmBlazor.Infrastructure.MailTm.Mappers;
using Microsoft.Extensions.Logging;

namespace MailTmBlazor.Infrastructure.MailTm;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

public sealed class MailTmHttpClient : IAuthService, IMailboxService
{
    private readonly IAccountSession _session;
    private readonly HttpClient _http;
    private readonly ILogger<MailTmHttpClient> _logger;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    private string? _sessionToken;
    private AccountSnapshot? _lastSnapshot;

    public MailTmHttpClient(IAccountSession session, HttpClient http, ILogger<MailTmHttpClient> logger)
    {
        _session = session;
        _http = http;
        _logger = logger;
    }

    // --- Auth ---
    
    

    public async Task<Account> RegisterAsync(string address, string password)
    {
        var res = await _http.PostAsJsonAsync("accounts", new { address, password }, Json);

        if (res.StatusCode == HttpStatusCode.Conflict)
        {
            _logger.LogInformation("Account {Address} already exists; continuing to login", address);
            // account already exists → pretend success so the flow can continue to LoginAsync
            return new Account(id: "", address, quota: 0, used: 0, isDisabled: false, isDeleted: false,
                createdAt: DateTime.UtcNow, updatedAt: DateTime.UtcNow);
        }

        await EnsureSuccessAsync(res);
        var dto = await res.Content.ReadFromJsonAsync<AccountDto>(Json) ?? throw new("No account payload");
        return dto.ToDomain();
    }
    

    public async Task LoginAsync(string address, string password)
    {
        // 1) token
        var tokRes = await _http.PostAsJsonAsync("token", new { address, password }, Json);
        await EnsureSuccessAsync(tokRes);
        var tokenDto = await tokRes.Content.ReadFromJsonAsync<TokenDto>(Json) ?? throw new("Auth failed");
        _sessionToken = tokenDto.Token;
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _sessionToken);

        // 2) me
        var meRes = await _http.GetAsync("me");
        await EnsureSuccessAsync(meRes);
        var me = await meRes.Content.ReadFromJsonAsync<AccountDto>(Json) ?? throw new("No /me data");

        // 3) persist combined snapshot
        await _session.SetAsync(new AccountSnapshot(
            _sessionToken, me.Id, me.Address, me.Quota, me.Used, me.IsDisabled, me.IsDeleted, me.CreatedAt, me.UpdatedAt
        ));
        _lastSnapshot = await _session.GetAsync();
    }

    public async Task<Account> LoginAndFetchAsync(string address, string password)
    {
        await LoginAsync(address, password);
        return await MeAsync();
    }

    public async Task DeleteAccountAsync(string accountId)
    {
        await EnsureAuthAsync();
        var res = await _http.DeleteAsync($"accounts/{accountId}");
        if (!res.IsSuccessStatusCode)
        {
            // try fallback to /me delete
            var meDelete = await _http.DeleteAsync("me");
            await EnsureSuccessAsync(meDelete);
        }
        else
        {
            await EnsureSuccessAsync(res);
        }
        await _session.ClearAsync();
        _sessionToken = null;
        _lastSnapshot = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<Account> MeAsync()
    {
        await EnsureAuthAsync();
        var res = await _http.GetAsync("me");
        await EnsureSuccessAsync(res);
        var dto = await res.Content.ReadFromJsonAsync<AccountDto>(Json) ?? throw new("No /me data");
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
        var res = await _http.GetAsync($"domains?page={page}");
        await EnsureSuccessAsync(res);
        var hydra = await res.Content.ReadFromJsonAsync<HydraPage<DomainDto>>(Json);
        return (hydra?.Items ?? []).Select(d => d.ToDomain()).ToList();
    }

    public async Task<PagedResult<Message>> GetMessagesAsync(int page = 1)
    {
        await EnsureAuthAsync();
        var res = await _http.GetAsync($"messages?page={page}");
        await EnsureSuccessAsync(res);
        var hydra = await res.Content.ReadFromJsonAsync<HydraPage<MessageListItemDto>>(Json);
        var items = (hydra?.Items ?? []).Select(m => m.ToDomain()).ToList();
        return new(items, hydra?.TotalItems ?? items.Count);
    }

    public async Task<Message> GetMessageAsync(string id)
    {
        await EnsureAuthAsync();
        var res = await _http.GetAsync($"messages/{id}");
        await EnsureSuccessAsync(res);
        var dto = await res.Content.ReadFromJsonAsync<MessageDetailDto>(Json) ?? throw new("Not found");
        return dto.ToDomain();
    }

    public async Task MarkSeenAsync(string id, bool seen = true)
    {
        await EnsureAuthAsync();
        var content = JsonContent.Create(
            new { seen },
            mediaType: new MediaTypeHeaderValue("application/merge-patch+json"),
            options: Json);
        var req = new HttpRequestMessage(HttpMethod.Patch, $"messages/{id}") { Content = content };
        var res = await _http.SendAsync(req);
        await EnsureSuccessAsync(res);
    }

    public async Task DeleteAsync(string id)
    {
        await EnsureAuthAsync();
        var res = await _http.DeleteAsync($"messages/{id}");
        await EnsureSuccessAsync(res);
    }

    // --- helper ---
    private async Task EnsureAuthAsync()
    {
        if (_http.DefaultRequestHeaders.Authorization is not null) return;

        var snap = _lastSnapshot ?? await _session.GetAsync();
        if (snap is not null)
        {
            _lastSnapshot = snap;
            _sessionToken = snap.Token;
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", snap.Token);
        }
        else
        {
            throw new InvalidOperationException("Not authenticated");
        }
    }

    private async Task EnsureSuccessAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        HydraError? hydra = null;
        try
        {
            hydra = await response.Content.ReadFromJsonAsync<HydraError>(Json);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse hydra error for {Method} {Uri}",
                response.RequestMessage?.Method, response.RequestMessage?.RequestUri);
        }

        var message = hydra?.Description ?? hydra?.Title;
        if (string.IsNullOrWhiteSpace(message))
        {
            message = await response.Content.ReadAsStringAsync();
        }

        _logger.LogWarning("mail.tm request {Method} {Uri} failed with {StatusCode}: {Message}",
            response.RequestMessage?.Method,
            response.RequestMessage?.RequestUri,
            (int)response.StatusCode,
            message);

        throw new MailTmApiException(response.StatusCode,
            string.IsNullOrWhiteSpace(message) ? "mail.tm request failed." : message);
    }
}
