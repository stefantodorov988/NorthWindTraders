namespace NorthWindTraders.Application.Customers.Overview;

public interface ICustomerOverviewQueryHandler
{
    Task<CustomerOverviewResult> HandleAsync(CustomerOverviewQuery query, CancellationToken cancellationToken = default);
}
