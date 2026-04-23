namespace NorthWindTraders.Application.Customers.Detail;

public sealed record CustomerDetailHeader(
    string CustomerId,
    string CompanyName,
    string? ContactName,
    string? ContactTitle);
