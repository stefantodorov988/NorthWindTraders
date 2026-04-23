namespace NorthWindTraders.Application.Customers.Overview;

public interface ICustomerOverviewDataAccess
{
    Task<CustomerOverviewResult> GetAsync(CustomerOverviewQuery query, CancellationToken cancellationToken = default);
}
