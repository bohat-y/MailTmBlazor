using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MercureProxy.Web.Realtime;

public sealed class MercureSubscriptionManager
{
    private readonly ConcurrentDictionary<string, Subscription> _subscriptions = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly HubService _hubService;
    private readonly ILogger<MercureSubscriptionManager> _logger;

    public MercureSubscriptionManager(
        IHttpClientFactory httpClientFactory,
        HubService hubService,
        ILogger<MercureSubscriptionManager> logger)
    {
        _httpClientFactory = httpClientFactory;
        _hubService = hubService;
        _logger = logger;
    }

    public void SetToken(string accountId, string token)
    {
        var subscription = _subscriptions.GetOrAdd(accountId, _ => new Subscription());
        lock (subscription.Sync)
        {
            subscription.Token = token;
        }
    }

    public Task RegisterConnectionAsync(string accountId, string connectionId)
    {
        var subscription = _subscriptions.GetOrAdd(accountId, _ => new Subscription());
        Task? workerToStart = null;

        lock (subscription.Sync)
        {
            subscription.ConnectionIds.Add(connectionId);

            if (subscription.Token is null)
            {
                throw new InvalidOperationException($"Mercure token for account '{accountId}' has not been registered.");
            }

            if (subscription.Worker is null || subscription.Worker.IsCompleted)
            {
                subscription.Cancellation = new CancellationTokenSource();
                subscription.Worker = Task.Run(() => RunAsync(accountId, subscription, subscription.Cancellation.Token));
                workerToStart = subscription.Worker;
            }
        }

        if (workerToStart is not null)
        {
            _logger.LogInformation("Starting Mercure subscription for {AccountId}", accountId);
        }

        return Task.CompletedTask;
    }

    public async Task UnregisterConnectionAsync(string accountId, string connectionId)
    {
        if (!_subscriptions.TryGetValue(accountId, out var subscription))
        {
            return;
        }

        lock (subscription.Sync)
        {
            subscription.ConnectionIds.Remove(connectionId);
            if (subscription.ConnectionIds.Count > 0)
            {
                return;
            }

            subscription.Cancellation?.Cancel();
            subscription.Cancellation?.Dispose();
            subscription.Cancellation = null;
            subscription.Worker = null;
            subscription.Token = null;
            _subscriptions.TryRemove(accountId, out _);
        }

        _logger.LogInformation("Stopped Mercure subscription for {AccountId}", accountId);
        await Task.CompletedTask;
    }

    private async Task RunAsync(string accountId, Subscription subscription, CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(1);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                string token;
                lock (subscription.Sync)
                {
                    token = subscription.Token ?? throw new InvalidOperationException($"Missing token for {accountId}");
                }

                await PumpAsync(accountId, token, cancellationToken);
                delay = TimeSpan.FromSeconds(1);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Mercure subscription for {AccountId} failed; retrying in {Delay}s", accountId, delay.TotalSeconds);
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, 30));
            }
        }
    }

    private async Task PumpAsync(string accountId, string token, CancellationToken cancellationToken)
    {
        var client = _httpClientFactory.CreateClient("mercure");
        var topic = $"/accounts/{accountId}";
        var url = $".well-known/mercure?topic={Uri.EscapeDataString(topic)}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.ParseAdd("text/event-stream");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("Mercure stream for {AccountId} failed with {Status}: {Body}",
                accountId,
                (int)response.StatusCode,
                body);
            response.EnsureSuccessStatusCode();
        }

        _logger.LogInformation("Mercure stream opened for {AccountId}", accountId);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream, Encoding.UTF8, false, 8 * 1024);

        var sb = new StringBuilder();
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync();
            if (line is null)
            {
                break;
            }

            if (line.Length == 0)
            {
                var block = sb.ToString();
                sb.Clear();

                foreach (var raw in block.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    var trimmed = raw.TrimEnd('\r');
                    if (!trimmed.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    var json = trimmed[5..].TrimStart();
                    if (json.Length > 0)
                    {
                        await _hubService.SendToGroupAsync(accountId, "mailUpdate", json, CancellationToken.None);
                    }
                }
                continue;
            }

            sb.Append(line).Append('\n');
        }

        _logger.LogInformation("Mercure stream for {AccountId} ended", accountId);
    }

    private sealed class Subscription
    {
        public object Sync { get; } = new();
        public string? Token { get; set; }
        public HashSet<string> ConnectionIds { get; } = new();
        public CancellationTokenSource? Cancellation { get; set; }
        public Task? Worker { get; set; }
    }
}
