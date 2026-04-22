using System.Text.Json;
using Confluent.Kafka;
using Microsoft.AspNetCore.SignalR;
using TradingEngine.Api.Hubs;
using TradingEngine.Shared.Models;

namespace TradingEngine.Api.Services;

public class TradeConsumer : BackgroundService
{
    private readonly IHubContext<TradingHub> _hubContext;
    private readonly ILogger<TradeConsumer> _logger;
    private readonly string _bootstrapServers = "localhost:19092";
    private readonly string _topic = "trades.executed";

    public TradeConsumer(IHubContext<TradingHub> hubContext, ILogger<TradeConsumer> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _bootstrapServers,
            GroupId = "api-gateway-trades-group",
            AutoOffsetReset = AutoOffsetReset.Earliest
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topic);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var cr = consumer.Consume(stoppingToken);
                var trade = JsonSerializer.Deserialize<Trade>(cr.Message.Value);

                if (trade != null)
                {
                    _logger.LogInformation($"Broadcasting trade: {trade.Id} for {trade.Symbol}");
                    await _hubContext.Clients.Group($"trades:{trade.Symbol}").SendAsync("TradeExecuted", trade, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Error consuming trades");
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
