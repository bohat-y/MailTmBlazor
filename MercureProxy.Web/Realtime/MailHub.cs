using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace MercureProxy.Web.Realtime;

public sealed class MailHub(MercureSubscriptionManager subscriptions, ILogger<MailHub> logger) : Hub
{
    private readonly MercureSubscriptionManager _subscriptions = subscriptions;
    private readonly ILogger<MailHub> _logger = logger;

    public override async Task OnConnectedAsync()
    {
        var accountId = Context.User?.FindFirst("accountId")?.Value;
        if (string.IsNullOrWhiteSpace(accountId))
        {
            _logger.LogWarning("Connection {ConnectionId} missing accountId claim; aborting", Context.ConnectionId);
            Context.Abort();
            return;
        }

        Context.Items["accountId"] = accountId;
        await Groups.AddToGroupAsync(Context.ConnectionId, accountId);
        await _subscriptions.RegisterConnectionAsync(accountId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("accountId", out var value) && value is string accountId)
        {
            await _subscriptions.UnregisterConnectionAsync(accountId, Context.ConnectionId);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
