namespace NorthWindTraders.Infrastructure.Persistence.Entities;

public sealed class OrderDetailEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    /// <summary>Northwind-style fraction off (e.g. 0.1 = 10%).</summary>
    public decimal Discount { get; set; }
}
