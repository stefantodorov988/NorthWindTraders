using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NorthWindTraders.Application.Customers.Detail;
using NorthWindTraders.Infrastructure.Persistence;
using NorthWindTraders.Infrastructure.Persistence.Entities;
using Xunit;

namespace NorthWindTraders.Application.Tests.Customers.Detail;

public sealed class CustomerDetailQueryHandlerTests
{
    /// <summary>
    /// (10 * 2 * (1 - 0.1)) + (20 * 1 * (1 - 0)) + freight 5
    /// </summary>
    public const decimal ExpectedOrderTotal = 10m * 2m * 0.9m + 20m * 1m * 1m + 5m;

    public const int ExpectedDistinctProductTypes = 2;

    [Fact]
    public async Task HandleAsync_single_order_computes_line_discounts_freight_and_distinct_products()
    {
        await using var db = CreateContext();
        const string customerId = "TEST1";

        db.Customers.Add(
            new CustomerEntity
            {
                CustomerId = customerId,
                CompanyName = "Test Co",
                ContactName = "Tester",
                ContactTitle = "QA",
            });

        db.Orders.Add(
            new OrderEntity
            {
                OrderId = 1,
                CustomerId = customerId,
                Freight = 5m,
            });

        db.OrderDetails.AddRange(
            new OrderDetailEntity
            {
                OrderId = 1,
                ProductId = 1,
                UnitPrice = 10m,
                Quantity = 2,
                Discount = 0.1m,
            },
            new OrderDetailEntity
            {
                OrderId = 1,
                ProductId = 2,
                UnitPrice = 20m,
                Quantity = 1,
                Discount = 0m,
            });

        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.HandleAsync(new CustomerDetailQuery(customerId, OrderPage: 1, OrderPageSize: 10));

        result.Customer.Should().NotBeNull();
        result.Customer!.Should().BeEquivalentTo(
            new CustomerDetailHeader(customerId, "Test Co", "Tester", "QA"));

        result.OrderSummaries.Should().ContainSingle();
        var summary = result.OrderSummaries.Single();
        summary.OrderId.Should().Be(1);
        summary.Freight.Should().Be(5m);
        summary.TotalOrderValue.Should().Be(ExpectedOrderTotal);
        summary.DistinctProductTypeCount.Should().Be(ExpectedDistinctProductTypes);
        result.OrderTotalCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_pagination_first_page_returns_two_orders_and_total_three()
    {
        await using var db = CreateContext();
        const string customerId = "PG001";

        db.Customers.Add(
            new CustomerEntity
            {
                CustomerId = customerId,
                CompanyName = "Paged Trading",
                ContactName = "Pat Pager",
                ContactTitle = "Buyer",
            });

        for (var orderId = 1; orderId <= 3; orderId++)
        {
            db.Orders.Add(
                new OrderEntity
                {
                    OrderId = orderId,
                    CustomerId = customerId,
                    Freight = 0m,
                });

            db.OrderDetails.Add(
                new OrderDetailEntity
                {
                    OrderId = orderId,
                    ProductId = 100 + orderId,
                    UnitPrice = 1m,
                    Quantity = 1,
                    Discount = 0m,
                });
        }

        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.HandleAsync(new CustomerDetailQuery(customerId, OrderPage: 1, OrderPageSize: 2));

        result.Customer.Should().NotBeNull();
        result.Customer!.Should().BeEquivalentTo(
            new CustomerDetailHeader(customerId, "Paged Trading", "Pat Pager", "Buyer"));

        result.OrderSummaries.Should().HaveCount(2, "first page with page size 2");
        result.OrderTotalCount.Should().Be(3);
        result.OrderPage.Should().Be(1);
        result.OrderPageSize.Should().Be(2);
        result.OrderSummaries.Select(s => s.OrderId).Should().Equal(1, 2);
    }

    private static NorthwindDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NorthwindDbContext>()
            .UseInMemoryDatabase($"CustomerDetailTests_{Guid.NewGuid():N}")
            .Options;

        return new NorthwindDbContext(options);
    }

    private static CustomerDetailQueryHandler CreateSut(NorthwindDbContext db)
    {
        var dataAccess = new EfCustomerDetailDataAccess(db);
        return new CustomerDetailQueryHandler(dataAccess);
    }
}
