using Microsoft.AspNetCore.SignalR;
using TradingEngine.Shared.Models;

namespace TradingEngine.Api.Hubs;

public class TradingHub : Hub
{
    public async Task SubscribeToSymbol(string symbol)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"orderbook:{symbol}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"trades:{symbol}");
    }

    public async Task BroadcastOrderBookUpdate(OrderBookSnapshot snapshot)
    {
        await Clients.Group($"orderbook:{snapshot.Symbol}")
            .SendAsync("OrderBookUpdated", snapshot);
    }

    public async Task BroadcastTrade(Trade trade)
    {
        await Clients.Group($"trades:{trade.Symbol}")
            .SendAsync("TradeExecuted", trade);
    }
}
