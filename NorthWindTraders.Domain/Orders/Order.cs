using NorthWindTraders.Domain.Customers;

namespace NorthWindTraders.Domain.Orders;

/// <summary>
/// Order as a domain concept (header-level facts). Line items are modeled separately in queries/projections.
/// </summary>
public sealed class Order
{
    public Order(
        OrderId id,
        CustomerId customerId,
        DateTime? orderDate,
        decimal freight)
    {
        Id = id;
        CustomerId = customerId;
        OrderDate = orderDate;
        Freight = freight;
    }

    public OrderId Id { get; }
    public CustomerId CustomerId { get; }
    public DateTime? OrderDate { get; }
    public decimal Freight { get; }
}
