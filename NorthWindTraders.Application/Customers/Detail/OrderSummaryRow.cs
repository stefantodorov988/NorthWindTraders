namespace NorthWindTraders.Application.Customers.Detail;

public sealed record OrderSummaryRow(
    int OrderId,
    decimal TotalOrderValue,
    int DistinctProductTypeCount,
    decimal Freight);
