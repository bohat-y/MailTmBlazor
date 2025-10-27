using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.SignalR.Management;

namespace MercureProxy.Web.Realtime;

public sealed class HubService : IAsyncDisposable
{
    private readonly ServiceHubContext _hubContext;

    public HubService(ServiceHubContext hubContext)
    {
        _hubContext = hubContext;
    }

    public Task<NegotiationResponse> NegotiateAsync(NegotiationOptions options)
        => _hubContext.NegotiateAsync(options).AsTask();

    public Task SendToGroupAsync(string group, string method, object? arg, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group(group).SendAsync(method, arg, cancellationToken);

    public async ValueTask DisposeAsync()
    {
        await _hubContext.DisposeAsync();
    }
}
