using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NorthWindTraders.Application.Customers.Overview;
using NorthWindTraders.Infrastructure.Persistence;
using NorthWindTraders.Infrastructure.Persistence.Entities;
using Xunit;

namespace NorthWindTraders.Application.Tests.Customers.Overview;

public sealed class CustomerOverviewQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_partial_search_alf_matches_Alfreds_Futterkiste()
    {
        await using var db = CreateContext();
        db.Customers.AddRange(
            new CustomerEntity
            {
                CustomerId = "ALFKI",
                CompanyName = "Alfreds Futterkiste",
                ContactName = "Maria Anders",
                ContactTitle = "Sales Representative",
            },
            new CustomerEntity
            {
                CustomerId = "NOISE",
                CompanyName = "Contoso Wholesale",
                ContactName = "Bob Smith",
                ContactTitle = "Owner",
            });
        db.Orders.Add(new OrderEntity { OrderId = 1, CustomerId = "ALFKI" });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.HandleAsync(new CustomerOverviewQuery(Search: "alf", Page: 1, PageSize: 10));

        result.Items.Should().ContainSingle(c => c.CompanyName == "Alfreds Futterkiste");
        result.Items.Should().NotContain(c => c.CustomerId == "NOISE");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_case_insensitive_search_ANNA_matches_Annas_Food_Market()
    {
        await using var db = CreateContext();
        db.Customers.AddRange(
            new CustomerEntity
            {
                CustomerId = "ANNA1",
                CompanyName = "Anna's Food Market",
                ContactName = "Helen Vendor",
                ContactTitle = "Manager",
            },
            new CustomerEntity
            {
                CustomerId = "OTHER",
                CompanyName = "Globex Corporation",
                ContactName = "John Doe",
                ContactTitle = "VP",
            });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var result = await sut.HandleAsync(new CustomerOverviewQuery(Search: "ANNA", Page: 1, PageSize: 10));

        result.Items.Should().ContainSingle(c => c.CompanyName == "Anna's Food Market");
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task HandleAsync_pagination_returns_correct_page_and_total()
    {
        await using var db = CreateContext();
        db.Customers.AddRange(
            Customer("A", "Alpha Ltd"),
            Customer("B", "Bravo Ltd"),
            Customer("C", "Charlie Ltd"),
            Customer("D", "Delta Ltd"),
            Customer("E", "Echo Ltd"));
        db.Orders.AddRange(
            new OrderEntity { OrderId = 1, CustomerId = "A" },
            new OrderEntity { OrderId = 2, CustomerId = "B" },
            new OrderEntity { OrderId = 3, CustomerId = "C" },
            new OrderEntity { OrderId = 4, CustomerId = "D" },
            new OrderEntity { OrderId = 5, CustomerId = "E" });

        await db.SaveChangesAsync();

        var sut = CreateSut(db);
        var page2 = await sut.HandleAsync(new CustomerOverviewQuery(Search: null, Page: 2, PageSize: 2));

        page2.TotalCount.Should().Be(5);
        page2.Page.Should().Be(2);
        page2.PageSize.Should().Be(2);
        page2.Items.Should().HaveCount(2);
        page2.Items.Select(i => i.CompanyName).Should().Equal("Charlie Ltd", "Delta Ltd");
        page2.Items.Should().OnlyContain(c => c.OrderCount == 1);
    }

    private static CustomerEntity Customer(string id, string company) =>
        new()
        {
            CustomerId = id,
            CompanyName = company,
            ContactName = "Contact",
            ContactTitle = "Title",
        };

    private static NorthwindDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<NorthwindDbContext>()
            .UseInMemoryDatabase($"CustomerOverviewTests_{Guid.NewGuid():N}")
            .Options;

        return new NorthwindDbContext(options);
    }

    private static CustomerOverviewQueryHandler CreateSut(NorthwindDbContext db)
    {
        var dataAccess = new EfCustomerOverviewDataAccess(db);
        return new CustomerOverviewQueryHandler(dataAccess);
    }
}
