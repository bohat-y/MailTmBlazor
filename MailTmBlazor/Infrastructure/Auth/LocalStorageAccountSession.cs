namespace MailTmBlazor.Infrastructure.Auth;

using System.Text.Json;
using Microsoft.JSInterop;

public sealed class LocalStorageAccountSession(IJSRuntime js) : IAccountSession
{
    private const string Key = "account";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<AccountSnapshot?> GetAsync()
    {
        var raw = await js.InvokeAsync<string?>("localStorage.getItem", Key);
        if (string.IsNullOrWhiteSpace(raw)) return null;

        // Try our snapshot shape
        if (TryDeserialize(raw, out AccountSnapshot? snap)) return snap;

        // Fallback: if user ever logged in with the official app shape (token + account fields + @id/@context)
        try
        {
            using var doc = JsonDocument.Parse(raw);
            if (doc.RootElement.TryGetProperty("token", out var t))
            {
                var r = doc.RootElement;
                return new AccountSnapshot(
                    t.GetString()!,
                    r.GetProperty("id").GetString()!,
                    r.GetProperty("address").GetString()!,
                    r.GetProperty("quota").GetInt64(),
                    r.GetProperty("used").GetInt64(),
                    r.GetProperty("isDisabled").GetBoolean(),
                    r.GetProperty("isDeleted").GetBoolean(),
                    r.GetProperty("createdAt").GetDateTime(),
                    r.GetProperty("updatedAt").GetDateTime()
                );
            }
        }
        catch { /* ignore */ }

        return null;

        static bool TryDeserialize(string s, out AccountSnapshot? snapshot)
        {
            try { snapshot = System.Text.Json.JsonSerializer.Deserialize<AccountSnapshot>(s, Json); return snapshot is not null; }
            catch { snapshot = null; return false; }
        }
    }

    public Task SetAsync(AccountSnapshot snapshot) =>
        js.InvokeVoidAsync("localStorage.setItem", Key, JsonSerializer.Serialize(snapshot, Json)).AsTask();

    public Task ClearAsync() =>
        js.InvokeVoidAsync("localStorage.removeItem", Key).AsTask();
}