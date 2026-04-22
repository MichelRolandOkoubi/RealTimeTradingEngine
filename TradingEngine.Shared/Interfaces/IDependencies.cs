using System.Threading.Tasks;
using TradingEngine.Shared.Models;

namespace TradingEngine.Shared.Interfaces;

public interface IRedisOrderBook
{
    Task AddOrderAsync(Order order);
    Task<List<Order>> GetMatchingOrdersAsync(Order order);
    Task ExecuteTradeAsync(Order incoming, Order matched, decimal quantity);
    Task<OrderBookSnapshot> GetSnapshotAsync(string symbol);
}

public interface IEventPublisher
{
    Task PublishOrderPlacedAsync(Order order);
    Task PublishTradeExecutedAsync(Trade trade);
}
