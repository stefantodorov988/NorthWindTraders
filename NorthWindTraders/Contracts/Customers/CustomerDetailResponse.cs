using NorthWindTraders.Application.Customers.Detail;
using NorthWindTraders.Application.Pagination;

namespace NorthWindTraders.Contracts.Customers;

public sealed record CustomerDetailResponse(
    CustomerDetailDto Customer,
    int OrderPageNumber,
    int OrderPageSize,
    int OrderTotalCount,
    int OrderTotalPages,
    IReadOnlyList<OrderSummaryDto> Orders)
{
    public static CustomerDetailResponse From(CustomerDetailResult result, PageRequest orderPage)
    {
        var c = result.Customer!;
        return new CustomerDetailResponse(
            new CustomerDetailDto(c.CustomerId, c.CompanyName, c.ContactName, c.ContactTitle),
            orderPage.PageNumber,
            orderPage.PageSize,
            result.OrderTotalCount,
            PagedResult<OrderSummaryDto>.CalculateTotalPages(result.OrderTotalCount, orderPage.PageSize),
            result.OrderSummaries
                .Select(o => new OrderSummaryDto(o.OrderId, o.TotalOrderValue, o.DistinctProductTypeCount, o.Freight))
                .ToList());
    }
}
