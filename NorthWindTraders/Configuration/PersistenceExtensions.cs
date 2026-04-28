using Microsoft.EntityFrameworkCore;
using NorthWindTraders.Application.Customers.Detail;
using NorthWindTraders.Application.Customers.Overview;
using NorthWindTraders.Infrastructure.Persistence;

namespace NorthWindTraders.Configuration;

public static class PersistenceExtensions
{
    public static IServiceCollection AddNorthwindPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Northwind");
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException(
                "Missing required connection string 'ConnectionStrings:Northwind'. Configure a live SQL Server database.");

        services.AddDbContext<NorthwindDbContext>(options =>
        {
            options.UseSqlServer(connectionString);
        });

        services.AddScoped<ICustomerOverviewDataAccess, EfCustomerOverviewDataAccess>();
        services.AddScoped<ICustomerDetailDataAccess, EfCustomerDetailDataAccess>();
        services.AddScoped<ICustomerOverviewQueryHandler, CustomerOverviewQueryHandler>();
        services.AddScoped<ICustomerDetailQueryHandler, CustomerDetailQueryHandler>();

        return services;
    }
}
