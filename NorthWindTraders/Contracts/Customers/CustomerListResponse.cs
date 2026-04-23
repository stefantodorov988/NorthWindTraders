using NorthWindTraders.Application.Customers.Overview;
using NorthWindTraders.Application.Pagination;

namespace NorthWindTraders.Contracts.Customers;

public sealed record CustomerListResponse(
    int PageNumber,
    int PageSize,
    int TotalCount,
    int TotalPages,
    IReadOnlyList<CustomerOverviewItemDto> Items)
{
    public static CustomerListResponse From(CustomerOverviewResult result, PageRequest pageRequest) =>
        new(
            pageRequest.PageNumber,
            pageRequest.PageSize,
            result.TotalCount,
            PagedResult<CustomerOverviewItemDto>.CalculateTotalPages(result.TotalCount, pageRequest.PageSize),
            result.Items
                .Select(i => new CustomerOverviewItemDto(i.CustomerId, i.CompanyName, i.OrderCount))
                .ToList());
}
