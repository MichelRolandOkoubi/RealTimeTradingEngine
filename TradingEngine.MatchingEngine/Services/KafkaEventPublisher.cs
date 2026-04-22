using System.Text.Json;
using Confluent.Kafka;
using TradingEngine.Shared.Interfaces;
using TradingEngine.Shared.Models;

namespace TradingEngine.MatchingEngine.Services;

public class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private const string OrderPlacedTopic = "orders.placed";
    private const string TradeExecutedTopic = "trades.executed";

    public KafkaEventPublisher(string bootstrapServers)
    {
        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<string, string>(config).Build();
    }

    public async Task PublishOrderPlacedAsync(Order order)
    {
        var message = new Message<string, string>
        {
            Key = order.Id,
            Value = JsonSerializer.Serialize(order)
        };
        await _producer.ProduceAsync(OrderPlacedTopic, message);
    }

    public async Task PublishTradeExecutedAsync(Trade trade)
    {
        var message = new Message<string, string>
        {
            Key = trade.Id,
            Value = JsonSerializer.Serialize(trade)
        };
        await _producer.ProduceAsync(TradeExecutedTopic, message);
    }
}
