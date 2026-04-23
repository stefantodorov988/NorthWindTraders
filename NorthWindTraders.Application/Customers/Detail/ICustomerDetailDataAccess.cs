namespace NorthWindTraders.Application.Customers.Detail;

public interface ICustomerDetailDataAccess
{
    Task<CustomerDetailResult> GetAsync(CustomerDetailQuery query, CancellationToken cancellationToken = default);
}
