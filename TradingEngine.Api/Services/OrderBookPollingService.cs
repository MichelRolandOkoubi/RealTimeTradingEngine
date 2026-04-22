using TradingEngine.Shared.Models;
using TradingEngine.Shared.Interfaces;
using Microsoft.AspNetCore.SignalR;
using TradingEngine.Api.Hubs;

namespace TradingEngine.Api.Services;

public class OrderBookPollingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHubContext<TradingHub> _hubContext;
    private readonly ILogger<OrderBookPollingService> _logger;

    public OrderBookPollingService(
        IServiceProvider serviceProvider, 
        IHubContext<TradingHub> hubContext,
        ILogger<OrderBookPollingService> logger)
    {
        _serviceProvider = serviceProvider;
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var orderBook = scope.ServiceProvider.GetRequiredService<IRedisOrderBook>();
                    var snapshot = await orderBook.GetSnapshotAsync("BTC/USD");
                    
                    await _hubContext.Clients.Group("orderbook:BTC/USD").SendAsync("OrderBookUpdated", snapshot, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error polling order book");
            }
            
            await Task.Delay(1000, stoppingToken);
        }
    }
}
