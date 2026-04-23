namespace NorthWindTraders.Application.Customers.Overview;

public sealed class CustomerOverviewQueryHandler(ICustomerOverviewDataAccess dataAccess) : ICustomerOverviewQueryHandler
{
    private readonly ICustomerOverviewDataAccess _dataAccess = dataAccess;

    public Task<CustomerOverviewResult> HandleAsync(
        CustomerOverviewQuery query,
        CancellationToken cancellationToken = default) =>
        _dataAccess.GetAsync(query, cancellationToken);
}
