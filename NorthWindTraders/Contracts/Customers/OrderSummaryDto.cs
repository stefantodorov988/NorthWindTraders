namespace NorthWindTraders.Contracts.Customers;

public sealed record OrderSummaryDto(
    int OrderId,
    decimal TotalOrderValue,
    int DistinctProductTypeCount,
    decimal Freight);
