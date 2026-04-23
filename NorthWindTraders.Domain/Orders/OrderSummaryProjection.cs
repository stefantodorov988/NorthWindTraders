using NorthWindTraders.Domain.Customers;

namespace NorthWindTraders.Domain.Orders;

/// <summary>
/// Read-side projection for paginated order history: totals and distinct product types.
/// Populated by application/infrastructure from SQL aggregates; not an EF entity and not an API contract.
/// </summary>
public sealed record OrderSummaryProjection(
    OrderId OrderId,
    CustomerId CustomerId,
    decimal TotalOrderValue,
    int DistinctProductTypeCount,
    decimal Freight)
{
    /// <summary>
    /// Line net total: sum over lines of UnitPrice * Quantity * (1 - Discount). Freight is not included here.
    /// </summary>
    public decimal LineItemsNetTotal => TotalOrderValue - Freight;

    /// <summary>
    /// Builds the projection from persisted aggregates so total order value stays defined in the domain.
    /// </summary>
    public static OrderSummaryProjection FromAggregates(
        OrderId orderId,
        CustomerId customerId,
        decimal lineItemsNetTotal,
        decimal freight,
        int distinctProductTypeCount)
    {
        if (lineItemsNetTotal < 0)
            throw new ArgumentOutOfRangeException(nameof(lineItemsNetTotal));
        if (freight < 0)
            throw new ArgumentOutOfRangeException(nameof(freight));
        if (distinctProductTypeCount < 0)
            throw new ArgumentOutOfRangeException(nameof(distinctProductTypeCount));

        return new OrderSummaryProjection(
            orderId,
            customerId,
            lineItemsNetTotal + freight,
            distinctProductTypeCount,
            freight);
    }
}
