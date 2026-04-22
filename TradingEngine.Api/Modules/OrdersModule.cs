using System.Text.Json;
using Confluent.Kafka;
using TradingEngine.Shared.Models;
using TradingEngine.Shared.Interfaces;

namespace TradingEngine.Api.Modules;

public static class OrdersModule
{
    public static void MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/orders", async (Order order, IProducer<string, string> producer) =>
        {
            var message = new Message<string, string>
            {
                Key = order.Id,
                Value = JsonSerializer.Serialize(order)
            };
            await producer.ProduceAsync("orders.placed", message);
            return Results.Accepted($"/orders/{order.Id}", order);
        });

        app.MapGet("/orderbook", async (IRedisOrderBook orderBook, string symbol) =>
        {
            var snapshot = await orderBook.GetSnapshotAsync(symbol ?? "BTC/USD");
            return Results.Ok(snapshot);
        });
    }
}
