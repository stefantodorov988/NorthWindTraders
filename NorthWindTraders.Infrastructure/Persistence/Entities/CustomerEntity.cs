namespace NorthWindTraders.Infrastructure.Persistence.Entities;

public sealed class CustomerEntity
{
    public string CustomerId { get; set; } = null!;
    public string CompanyName { get; set; } = null!;
    public string? ContactName { get; set; }
    public string? ContactTitle { get; set; }
}
