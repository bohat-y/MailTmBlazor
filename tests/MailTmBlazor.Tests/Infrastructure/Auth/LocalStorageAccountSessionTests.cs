using System.Text.Json;
using MailTmBlazor.Infrastructure.Auth;
using MailTmBlazor.Tests.Infrastructure;

namespace MailTmBlazor.Tests.Infrastructure.Auth;

public class LocalStorageAccountSessionTests
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task GetAsync_returns_null_when_storage_is_empty()
    {
        var js = new FakeJsRuntime();
        var session = new LocalStorageAccountSession(js);

        var result = await session.GetAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SetAsync_persists_snapshot_and_GetAsync_reads_it_back()
    {
        var js = new FakeJsRuntime();
        var session = new LocalStorageAccountSession(js);
        var snapshot = CreateSnapshot();

        await session.SetAsync(snapshot);

        Assert.True(js.Storage.TryGetValue("account", out var raw));
        Assert.False(string.IsNullOrWhiteSpace(raw));

        var roundTrip = await session.GetAsync();
        Assert.Equal(snapshot, roundTrip);
    }

    [Fact]
    public async Task GetAsync_parses_official_payload_shape_when_snapshot_deserialization_fails()
    {
        var js = new FakeJsRuntime();
        var payload = new
        {
            token = "legacy-token",
            id = "legacy-id",
            address = "legacy@example.com",
            quota = 1000,
            used = 5,
            isDisabled = false,
            isDeleted = false,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };
        js.Seed("account", JsonSerializer.Serialize(payload, Json));
        var session = new LocalStorageAccountSession(js);

        var result = await session.GetAsync();

        Assert.NotNull(result);
        Assert.Equal(payload.token, result!.Token);
        Assert.Equal(payload.address, result.Address);
    }

    [Fact]
    public async Task ClearAsync_removes_snapshot_from_storage()
    {
        var js = new FakeJsRuntime();
        var session = new LocalStorageAccountSession(js);
        await session.SetAsync(CreateSnapshot());

        await session.ClearAsync();

        Assert.False(js.Storage.ContainsKey("account"));
    }

    private static AccountSnapshot CreateSnapshot() =>
        new(
            "token-123",
            "acc-1",
            "user@example.com",
            10,
            1,
            false,
            false,
            DateTime.UtcNow.AddMinutes(-5),
            DateTime.UtcNow);
}
