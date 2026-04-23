namespace NorthWindTraders.Contracts.Customers;

public sealed record CustomerDetailDto(
    string CustomerId,
    string CompanyName,
    string? ContactName,
    string? ContactTitle);
