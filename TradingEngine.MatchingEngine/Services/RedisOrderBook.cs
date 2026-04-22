using System.Text.Json;
using StackExchange.Redis;
using TradingEngine.Shared.Interfaces;
using TradingEngine.Shared.Models;

namespace TradingEngine.MatchingEngine.Services;

public class RedisOrderBook : IRedisOrderBook
{
    private readonly IDatabase _db;
    private const string BidsKeyTemplate = "orderbook:{0}:bids";
    private const string AsksKeyTemplate = "orderbook:{0}:asks";
    private const string OrderKeyTemplate = "order:{0}";

    public RedisOrderBook(IConnectionMultiplexer redis)
    {
        _db = redis.GetDatabase();
    }

    public async Task AddOrderAsync(Order order)
    {
        var orderKey = string.Format(OrderKeyTemplate, order.Id);
        var sideKey = order.Side == OrderSide.Buy 
            ? string.Format(BidsKeyTemplate, order.Symbol) 
            : string.Format(AsksKeyTemplate, order.Symbol);

        var batch = _db.CreateBatch();
        
        // Store order details
        var hashEntries = new HashEntry[]
        {
            new("Id", order.Id),
            new("UserId", order.UserId),
            new("Side", order.Side.ToString()),
            new("Price", (double)order.Price),
            new("Quantity", (double)order.Quantity),
            new("RemainingQuantity", (double)order.RemainingQuantity),
            new("Status", order.Status.ToString()),
            new("CreatedAt", order.CreatedAt.Ticks)
        };
        
        _ = batch.HashSetAsync(orderKey, hashEntries);
        _ = batch.SortedSetAddAsync(sideKey, order.Id, (double)order.Price);
        
        batch.Execute();
    }

    public async Task<List<Order>> GetMatchingOrdersAsync(Order order)
    {
        var oppositeSideKey = order.Side == OrderSide.Buy 
            ? string.Format(AsksKeyTemplate, order.Symbol) 
            : string.Format(BidsKeyTemplate, order.Symbol);

        // For Buy: we want Asks with Price <= Order.Price (Ascending)
        // For Sell: we want Bids with Price >= Order.Price (Descending)
        
        RedisValue[] orderIds;
        if (order.Side == OrderSide.Buy)
        {
            // Get asks from lowest price to highest, where price <= limit
            orderIds = await _db.SortedSetRangeByScoreAsync(oppositeSideKey, 0, (double)order.Price, take: 50);
        }
        else
        {
            // Get bids from highest price to lowest, where price >= limit
            orderIds = await _db.SortedSetRangeByScoreAsync(oppositeSideKey, (double)order.Price, double.MaxValue, order: Order.Descending, take: 50);
        }

        var matchingOrders = new List<Order>();
        foreach (var id in orderIds)
        {
            var o = await GetOrderAsync(id.ToString()!);
            if (o != null && o.RemainingQuantity > 0)
            {
                matchingOrders.Add(o);
            }
        }

        return matchingOrders;
    }

    public async Task ExecuteTradeAsync(Order incoming, Order matched, decimal quantity)
    {
        incoming.RemainingQuantity -= quantity;
        matched.RemainingQuantity -= quantity;

        if (matched.RemainingQuantity == 0)
        {
            matched.Status = OrderStatus.Filled;
            var sideKey = matched.Side == OrderSide.Buy 
                ? string.Format(BidsKeyTemplate, matched.Symbol) 
                : string.Format(AsksKeyTemplate, matched.Symbol);
            
            _ = _db.SortedSetRemoveAsync(sideKey, matched.Id);
        }
        else
        {
            matched.Status = OrderStatus.PartiallyFilled;
        }

        await UpdateOrderAsync(matched);
        await UpdateOrderAsync(incoming);
    }

    public async Task<OrderBookSnapshot> GetSnapshotAsync(string symbol)
    {
        var bidsKey = string.Format(BidsKeyTemplate, symbol);
        var asksKey = string.Format(AsksKeyTemplate, symbol);

        var bidsRaw = await _db.SortedSetRangeByScoreWithScoresAsync(bidsKey, order: Order.Descending, take: 10);
        var asksRaw = await _db.SortedSetRangeByScoreWithScoresAsync(asksKey, take: 10);

        return new OrderBookSnapshot
        {
            Symbol = symbol,
            Bids = bidsRaw.Select(x => new OrderBookEntry((decimal)x.Score, GetQuantityForOrder(x.Element.ToString()!))).ToList(),
            Asks = asksRaw.Select(x => new OrderBookEntry((decimal)x.Score, GetQuantityForOrder(x.Element.ToString()!))).ToList(),
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
    }

    private decimal GetQuantityForOrder(string orderId)
    {
        var val = _db.HashGet(string.Format(OrderKeyTemplate, orderId), "RemainingQuantity");
        return val.IsNull ? 0 : (decimal)(double)val;
    }

    private async Task<Order?> GetOrderAsync(string orderId)
    {
        var entries = await _db.HashGetAllAsync(string.Format(OrderKeyTemplate, orderId));
        if (entries.Length == 0) return null;

        var dict = entries.ToDictionary(x => x.Name.ToString(), x => x.Value);
        return new Order
        {
            Id = dict["Id"].ToString(),
            UserId = dict["UserId"].ToString(),
            Side = Enum.Parse<OrderSide>(dict["Side"].ToString()),
            Price = (decimal)(double)dict["Price"],
            Quantity = (decimal)(double)dict["Quantity"],
            RemainingQuantity = (decimal)(double)dict["RemainingQuantity"],
            Status = Enum.Parse<OrderStatus>(dict["Status"].ToString()),
            CreatedAt = new DateTime((long)dict["CreatedAt"])
        };
    }

    private async Task UpdateOrderAsync(Order order)
    {
        var key = string.Format(OrderKeyTemplate, order.Id);
        await _db.HashSetAsync(key, new HashEntry[] {
            new("RemainingQuantity", (double)order.RemainingQuantity),
            new("Status", order.Status.ToString())
        });
    }
}
