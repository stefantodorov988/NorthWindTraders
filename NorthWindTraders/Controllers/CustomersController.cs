using Microsoft.AspNetCore.Mvc;
using NorthWindTraders.Application.Customers.Detail;
using NorthWindTraders.Application.Customers.Overview;
using NorthWindTraders.Application.Pagination;
using NorthWindTraders.Contracts.Customers;
using NorthWindTraders.ErrorHandling;

namespace NorthWindTraders.Controllers;

[ApiController]
[Route("customers")]
public sealed class CustomersController(
    ICustomerOverviewQueryHandler overviewHandler,
    ICustomerDetailQueryHandler detailHandler) : ControllerBase
{
    private const int DefaultPageSize = 25;

    private readonly ICustomerOverviewQueryHandler _overviewHandler = overviewHandler;
    private readonly ICustomerDetailQueryHandler _detailHandler = detailHandler;

    /// <summary>List customers with optional name search and pagination.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CustomerListResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<CustomerListResponse>> GetListAsync(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var pageRequest = PageRequest.Normalize(page, pageSize);
        var query = new CustomerOverviewQuery(search, pageRequest.PageNumber, pageRequest.PageSize);
        var result = await _overviewHandler.HandleAsync(query, cancellationToken);
        return Ok(CustomerListResponse.From(result, pageRequest));
    }

    /// <summary>Get customer details and a page of order summaries.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(CustomerDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CustomerDetailResponse>> GetByIdAsync(
        [FromRoute] string id,
        [FromQuery] int orderPage = 1,
        [FromQuery] int orderPageSize = DefaultPageSize,
        CancellationToken cancellationToken = default)
    {
        var pageRequest = PageRequest.Normalize(orderPage, orderPageSize);
        var query = new CustomerDetailQuery(id, pageRequest.PageNumber, pageRequest.PageSize);
        var result = await _detailHandler.HandleAsync(query, cancellationToken);

        if (result.Customer is null)
        {
            throw new ApiException(
                StatusCodes.Status404NotFound,
                ErrorCodes.NotFound,
                $"Customer '{id}' was not found.");
        }

        return Ok(CustomerDetailResponse.From(result, pageRequest));
    }
}
