namespace NorthWindTraders.Application.Customers.Overview;

public sealed record CustomerOverviewResult(
    IReadOnlyList<CustomerOverviewRow> Items,
    int Page,
    int PageSize,
    int TotalCount);
