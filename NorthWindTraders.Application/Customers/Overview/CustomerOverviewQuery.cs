namespace NorthWindTraders.Application.Customers.Overview;

public sealed record CustomerOverviewQuery(string? Search, int Page, int PageSize);
