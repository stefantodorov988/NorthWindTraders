namespace NorthWindTraders.Application.Customers.Detail;

public sealed record CustomerDetailResult(
    CustomerDetailHeader? Customer,
    IReadOnlyList<OrderSummaryRow> OrderSummaries,
    int OrderPage,
    int OrderPageSize,
    int OrderTotalCount);
