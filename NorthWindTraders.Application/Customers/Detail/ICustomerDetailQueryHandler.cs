namespace NorthWindTraders.Application.Customers.Detail;

public interface ICustomerDetailQueryHandler
{
    Task<CustomerDetailResult> HandleAsync(CustomerDetailQuery query, CancellationToken cancellationToken = default);
}
