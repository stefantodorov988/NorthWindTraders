namespace NorthWindTraders.Application.Customers.Detail;

public sealed class CustomerDetailQueryHandler(ICustomerDetailDataAccess dataAccess) : ICustomerDetailQueryHandler
{
    public const int MaxOrderPageSize = 100;

    private readonly ICustomerDetailDataAccess _dataAccess = dataAccess;

    public Task<CustomerDetailResult> HandleAsync(
        CustomerDetailQuery query,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query.CustomerId))
        {
            return Task.FromResult(new CustomerDetailResult(
                Customer: null,
                OrderSummaries: Array.Empty<OrderSummaryRow>(),
                query.OrderPage,
                query.OrderPageSize,
                OrderTotalCount: 0));
        }

        var page = Math.Max(1, query.OrderPage);
        var pageSize = Math.Clamp(query.OrderPageSize, 1, MaxOrderPageSize);
        var normalized = new CustomerDetailQuery(query.CustomerId.Trim(), page, pageSize);

        return _dataAccess.GetAsync(normalized, cancellationToken);
    }
}
