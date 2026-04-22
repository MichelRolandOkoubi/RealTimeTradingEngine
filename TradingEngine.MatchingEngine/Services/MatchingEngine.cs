using System.Threading.Tasks;
using TradingEngine.Shared.Interfaces;
using TradingEngine.Shared.Models;

namespace TradingEngine.MatchingEngine.Services;

public class MatchingEngine
{
    private readonly IRedisOrderBook _orderBook;
    private readonly IEventPublisher _eventPublisher;

    public MatchingEngine(IRedisOrderBook orderBook, IEventPublisher eventPublisher)
    {
        _orderBook = orderBook;
        _eventPublisher = eventPublisher;
    }

    public async Task ProcessOrderAsync(Order order)
    {
        // 1. Validate
        if (order.Quantity <= 0) return;

        order.RemainingQuantity = order.Quantity;
        order.Status = OrderStatus.Placed;

        // 2. Find matches
        var matchingOrders = await _orderBook.GetMatchingOrdersAsync(order);

        foreach (var match in matchingOrders)
        {
            if (order.RemainingQuantity <= 0) break;

            decimal fillQuantity = Math.Min(order.RemainingQuantity, match.RemainingQuantity);
            
            // Execute trade record
            var trade = new Trade
            {
                BuyOrderId = order.Side == OrderSide.Buy ? order.Id : match.Id,
                SellOrderId = order.Side == OrderSide.Sell ? order.Id : match.Id,
                Price = match.Price,
                Quantity = fillQuantity,
                Symbol = order.Symbol
            };

            // Update state in Redis
            await _orderBook.ExecuteTradeAsync(order, match, fillQuantity);

            // Publish event
            await _eventPublisher.PublishTradeExecutedAsync(trade);
        }

        // 3. Add remaining to book if it's a Limit order
        if (order.RemainingQuantity > 0 && order.Type == OrderType.Limit)
        {
            await _orderBook.AddOrderAsync(order);
        }
    }
}
