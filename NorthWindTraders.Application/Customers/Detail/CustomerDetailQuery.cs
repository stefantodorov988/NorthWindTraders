namespace NorthWindTraders.Application.Customers.Detail;

public sealed record CustomerDetailQuery(string CustomerId, int OrderPage, int OrderPageSize);
