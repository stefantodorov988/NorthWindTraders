using Microsoft.EntityFrameworkCore;
using NorthWindTraders.Infrastructure.Persistence.Entities;

namespace NorthWindTraders.Infrastructure.Persistence;

public sealed class NorthwindDbContext(DbContextOptions<NorthwindDbContext> options) : DbContext(options)
{
    public DbSet<CustomerEntity> Customers => Set<CustomerEntity>();
    public DbSet<OrderEntity> Orders => Set<OrderEntity>();
    public DbSet<OrderDetailEntity> OrderDetails => Set<OrderDetailEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var customer = modelBuilder.Entity<CustomerEntity>();
        customer.HasKey(c => c.CustomerId);
        customer.Property(c => c.CustomerId).HasMaxLength(5);
        customer.Property(c => c.CompanyName).HasMaxLength(40);
        customer.Property(c => c.ContactName).HasMaxLength(30);
        customer.Property(c => c.ContactTitle).HasMaxLength(30);

        var order = modelBuilder.Entity<OrderEntity>();
        order.HasKey(o => o.OrderId);
        order.Property(o => o.CustomerId).HasMaxLength(5);
        order.Property(o => o.Freight).HasPrecision(18, 4);

        var detail = modelBuilder.Entity<OrderDetailEntity>();
        detail.HasKey(d => new { d.OrderId, d.ProductId });
        detail.Property(d => d.UnitPrice).HasPrecision(18, 4);
        detail.Property(d => d.Discount).HasPrecision(18, 4);
    }
}
