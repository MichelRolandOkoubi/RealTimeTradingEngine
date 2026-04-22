using System;

namespace TradingEngine.Shared.Models;

public record Order
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string UserId { get; init; } = default!;
    public string Symbol { get; init; } = "BTC/USD";
    public OrderSide Side { get; init; }
    public OrderType Type { get; init; }
    public decimal Price { get; init; }
    public decimal Quantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public record Trade
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string BuyOrderId { get; init; } = default!;
    public string SellOrderId { get; init; } = default!;
    public string Symbol { get; init; } = "BTC/USD";
    public decimal Price { get; init; }
    public decimal Quantity { get; init; }
    public DateTime ExecutedAt { get; init; } = DateTime.UtcNow;
}

public record OrderBookEntry(decimal Price, decimal Quantity);

public record OrderBookSnapshot
{
    public string Symbol { get; init; } = "BTC/USD";
    public List<OrderBookEntry> Bids { get; init; } = new();
    public List<OrderBookEntry> Asks { get; init; } = new();
    public decimal LastPrice { get; init; }
    public long Timestamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}
