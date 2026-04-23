namespace NorthWindTraders.Domain.Orders;

public readonly record struct OrderId(int Value)
{
    public static OrderId From(int value)
    {
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Order id must be positive.");

        return new OrderId(value);
    }
}
