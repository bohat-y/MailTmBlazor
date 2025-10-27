using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace MailTmBlazor.Infrastructure.Realtime;

public sealed class InboxRealtimeClient(
    IHttpClientFactory httpClientFactory,
    ILogger<InboxRealtimeClient> logger) : IAsyncDisposable
{
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
    private readonly ILogger<InboxRealtimeClient> _logger = logger;
    private HubConnection? _connection;

    public async Task StartAsync(string baseUrl, string accountId, string token, Func<Task> onUpdate)
    {
        await StopAsync();

        var negotiateRequest = new
        {
            accountId,
            token
        };

        var http = _httpClientFactory.CreateClient(nameof(InboxRealtimeClient));
        http.BaseAddress = new Uri(baseUrl);
        using var response = await http.PostAsJsonAsync("/api/negotiate", negotiateRequest);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<NegotiateResponseDto>();
        if (payload is null)
        {
            throw new InvalidOperationException("Invalid negotiate response");
        }

        var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(payload.Url), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(payload.AccessToken)!;
            })
            .WithAutomaticReconnect()
            .Build();

        connection.On<string>("mailUpdate", async _ =>
        {
            try
            {
                await onUpdate();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing mail update");
            }
        });

        await connection.StartAsync();
        _connection = connection;
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private async Task StopAsync()
    {
        if (_connection is null)
        {
            return;
        }

        try
        {
            await _connection.StopAsync();
            await _connection.DisposeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error while stopping SignalR connection");
        }
        finally
        {
            _connection = null;
        }
    }

    private sealed record NegotiateResponseDto(string Url, string AccessToken);
}
