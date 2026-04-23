namespace NorthWindTraders.Infrastructure.Persistence.Entities;

public sealed class OrderEntity
{
    public int OrderId { get; set; }
    public string CustomerId { get; set; } = null!;
    public decimal Freight { get; set; }
}
