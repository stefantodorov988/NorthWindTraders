using Microsoft.EntityFrameworkCore;
using NorthWindTraders.Application.Customers.Detail;

namespace NorthWindTraders.Infrastructure.Persistence;

public sealed class EfCustomerDetailDataAccess(NorthwindDbContext db) : ICustomerDetailDataAccess
{
    private readonly NorthwindDbContext _db = db;

    public async Task<CustomerDetailResult> GetAsync(
        CustomerDetailQuery query,
        CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers
            .AsNoTracking()
            .Where(c => c.CustomerId == query.CustomerId)
            .Select(c => new CustomerDetailHeader(
                c.CustomerId,
                c.CompanyName,
                c.ContactName,
                c.ContactTitle))
            .FirstOrDefaultAsync(cancellationToken);

        if (customer is null)
        {
            return new CustomerDetailResult(
                Customer: null,
                OrderSummaries: Array.Empty<OrderSummaryRow>(),
                query.OrderPage,
                query.OrderPageSize,
                OrderTotalCount: 0);
        }

        // Restrict line aggregation to this customer's orders only (avoids scanning all [Order Details] at scale).
        var detailsForCustomer =
            from d in _db.OrderDetails.AsNoTracking()
            join o in _db.Orders.AsNoTracking() on d.OrderId equals o.OrderId
            where o.CustomerId == query.CustomerId
            select d;

        var aggregates =
            from d in detailsForCustomer
            group d by d.OrderId
            into g
            select new
            {
                OrderId = g.Key,
                LineNet = g.Sum(d => d.UnitPrice * d.Quantity * (1 - d.Discount)),
                DistinctProductTypes = g.Select(d => d.ProductId).Distinct().Count(),
            };

        var orderRows =
            from o in _db.Orders.AsNoTracking()
            where o.CustomerId == query.CustomerId
            join a in aggregates on o.OrderId equals a.OrderId
            select new
            {
                o.OrderId,
                TotalOrderValue = a.LineNet + o.Freight,
                DistinctProductTypes = a.DistinctProductTypes,
                o.Freight,
            };

        var ordered = orderRows.OrderBy(r => r.OrderId);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var items = await ordered
            .Skip((query.OrderPage - 1) * query.OrderPageSize)
            .Take(query.OrderPageSize)
            .Select(r => new OrderSummaryRow(
                r.OrderId,
                r.TotalOrderValue,
                r.DistinctProductTypes,
                r.Freight))
            .ToListAsync(cancellationToken);

        return new CustomerDetailResult(customer, items, query.OrderPage, query.OrderPageSize, totalCount);
    }
}
