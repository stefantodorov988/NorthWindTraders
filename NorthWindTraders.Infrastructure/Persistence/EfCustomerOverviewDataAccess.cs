using Microsoft.EntityFrameworkCore;
using NorthWindTraders.Application.Customers.Overview;
using NorthWindTraders.Infrastructure.Persistence.Entities;

namespace NorthWindTraders.Infrastructure.Persistence;

public sealed class EfCustomerOverviewDataAccess(NorthwindDbContext db) : ICustomerOverviewDataAccess
{
    private readonly NorthwindDbContext _db = db;

    public async Task<CustomerOverviewResult> GetAsync(
        CustomerOverviewQuery query,
        CancellationToken cancellationToken = default)
    {
        var customers = _db.Customers.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            customers = customers.Where(c =>
                c.CompanyName.ToLower().Contains(term)
                || (c.ContactName != null && c.ContactName.ToLower().Contains(term)));
        }

        // Projected shape only; order count via translated COUNT subquery (index Orders.CustomerId for scale).
        var rowsQuery = customers.Select(c => new
        {
            c.CustomerId,
            c.CompanyName,
            OrderCount = _db.Orders.Count(o => o.CustomerId == c.CustomerId),
        });

        var ordered = rowsQuery
            .OrderBy(r => r.CompanyName)
            .ThenBy(r => r.CustomerId);

        var totalCount = await ordered.CountAsync(cancellationToken);

        var rows = await ordered
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(r => new CustomerOverviewRow(r.CustomerId, r.CompanyName, r.OrderCount))
            .ToListAsync(cancellationToken);

        return new CustomerOverviewResult(rows, query.Page, query.PageSize, totalCount);
    }
}
