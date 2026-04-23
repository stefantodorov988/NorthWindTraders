namespace NorthWindTraders.Contracts.Customers;

public sealed record CustomerOverviewItemDto(string CustomerId, string CompanyName, int OrderCount);
