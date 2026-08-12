using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.Customers;
using ExItS.PinoyBusinessPOS.Application.Expenses;
using ExItS.PinoyBusinessPOS.Application.Inventory;
using ExItS.PinoyBusinessPOS.Application.Payments;
using ExItS.PinoyBusinessPOS.Application.Sales;
using ExItS.PinoyBusinessPOS.Application.Statements;
using ExItS.PinoyBusinessPOS.Application.Purchasing;
using ExItS.PinoyBusinessPOS.Application.Returns;
using ExItS.PinoyBusinessPOS.Application.Permissions;
using ExItS.PinoyBusinessPOS.Domain.Abstractions;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ExItS.PinoyBusinessPOS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddPosPersistence(
        this IServiceCollection services,
        IConfiguration config)
    {
        var connectionString = config.GetConnectionString("PosDatabase")
            ?? throw new InvalidOperationException("Connection string 'PosDatabase' is not configured.");

        services.AddDbContext<PosDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.AddScoped<IPOSCustomerRepository, POSCustomerRepository>();
        services.AddScoped<ICreditEntryRepository, CreditEntryRepository>();
        services.AddScoped<ICreditDueDateChangeRepository, CreditDueDateChangeRepository>();
        services.AddScoped<IRepaymentRepository, RepaymentRepository>();
        services.AddScoped<IPaymentAttemptRepository, PaymentAttemptRepository>();
        services.AddSingleton<IPaymentGateway, FakePaymentGateway>();
        services.AddScoped<IProductCategoryRepository, ProductCategoryRepository>();
        services.AddScoped<ICatalogProductRepository, CatalogProductRepository>();
        services.AddScoped<ICatalogImportJobRepository, CatalogImportJobRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISaleReturnRepository, SaleReturnRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryReorderChangeRepository, InventoryReorderChangeRepository>();
        services.AddScoped<IStockCountRepository, StockCountRepository>();
        services.AddScoped<ICashierShiftRepository, CashierShiftRepository>();
        services.AddScoped<IPosRoleAssignmentRepository, PosRoleAssignmentRepository>();
        services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IRegisterRepository, RegisterRepository>();
        services.AddScoped<IPosOperationalSetupRepository, OperationalSetupRepository>();
        services.AddScoped<IPurchaseOrderRepository, PurchaseOrderRepository>();
        services.AddScoped<ISaleStockService, SaleStockService>();
        services.AddScoped<ISaleReturnStockService, SaleReturnStockService>();
        services.AddScoped<IPurchaseStockService, PurchaseStockService>();
        services.AddScoped<IUtangLedgerQuery, UtangLedgerQuery>();
        services.AddScoped<ILinkedCustomerRecentActivityQuery, LinkedCustomerRecentActivityQuery>();
        services.AddScoped<IOutstandingBalanceService, OutstandingBalanceService>();
        services.AddScoped<IPosUnitOfWork, PosUnitOfWork>();
        services.AddScoped<ExItS.PinoyBusinessPOS.Application.Abstractions.IPosIdempotencyService, Idempotency.PosIdempotencyService>();
        services.AddSingleton<IClock, SystemClock>();

        return services;
    }
}
