using Microsoft.AspNetCore.SignalR;

namespace FinancialMonitor.Api.Hubs;

/// <summary>
/// SignalR hub for broadcasting transaction updates to connected clients.
/// Client method: "ReceiveTransaction" — receives a single transaction object.
/// </summary>
public sealed class TransactionHub : Hub
{
    private readonly ILogger<TransactionHub> _logger;

    public TransactionHub(ILogger<TransactionHub> logger)
    {
        _logger = logger;
    }

    public override Task OnConnectedAsync()
    {
        _logger.LogInformation("Client connected: {ConnectionId}", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("Client disconnected: {ConnectionId}", Context.ConnectionId);
        return base.OnDisconnectedAsync(exception);
    }
}
