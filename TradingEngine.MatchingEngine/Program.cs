using System.Text.Json;
using Confluent.Kafka;
using StackExchange.Redis;
using TradingEngine.MatchingEngine.Services;
using TradingEngine.Shared.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

Console.WriteLine("Starting Trading Engine...");

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        var redisConn = ConnectionMultiplexer.Connect("localhost:6379");
        services.AddSingleton<IConnectionMultiplexer>(redisConn);
        services.AddSingleton<TradingEngine.Shared.Interfaces.IRedisOrderBook, RedisOrderBook>();
        services.AddSingleton<TradingEngine.Shared.Interfaces.IEventPublisher>(new KafkaEventPublisher("localhost:19092"));
        services.AddSingleton<TradingEngine.MatchingEngine.Services.MatchingEngine>();
    })
    .Build();

var matchingEngine = host.Services.GetRequiredService<TradingEngine.MatchingEngine.Services.MatchingEngine>();

var config = new ConsumerConfig
{
    BootstrapServers = "localhost:19092",
    GroupId = "matching-engine-group",
    AutoOffsetReset = AutoOffsetReset.Earliest
};

using var consumer = new ConsumerBuilder<string, string>(config).Build();
consumer.Subscribe("orders.placed");

Console.WriteLine("Engine Ready. Waiting for orders...");

CancellationTokenSource cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    cts.Cancel();
};

try
{
    while (!cts.IsCancellationRequested)
    {
        var cr = consumer.Consume(cts.Token);
        var order = JsonSerializer.Deserialize<Order>(cr.Message.Value);
        
        if (order != null)
        {
            Console.WriteLine($"Processing Order: {order.Id} - {order.Side} {order.Quantity} @ {order.Price}");
            await matchingEngine.ProcessOrderAsync(order);
        }
    }
}
catch (OperationCanceledException)
{
    // Normal shutdown
}
finally
{
    consumer.Close();
}
