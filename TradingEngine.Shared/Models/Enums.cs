namespace TradingEngine.Shared.Models;

public enum OrderSide
{
    Buy,
    Sell
}

public enum OrderType
{
    Market,
    Limit
}

public enum OrderStatus
{
    Placed,
    PartiallyFilled,
    Filled,
    Cancelled,
    Rejected
}
