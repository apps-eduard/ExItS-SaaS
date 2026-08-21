using ExItS.PinoyBusinessPOS.Application.CashierShifts;
using ExItS.PinoyBusinessPOS.Application.Catalog;
using ExItS.PinoyBusinessPOS.Application.Credit;
using ExItS.PinoyBusinessPOS.Application.ConnectedSuppliers;
using ExItS.PinoyBusinessPOS.Application.CustomerOrdering;
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
using ExItS.PinoyBusinessPOS.Infrastructure.Media;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.Repositories;
using ExItS.PinoyBusinessPOS.Infrastructure.Persistence.ConnectedSuppliers;
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
        services.AddScoped<ICatalogProductUnitRepository, CatalogProductUnitRepository>();
        services.AddScoped<ICatalogProductImageRepository, CatalogProductImageRepository>();
        services.Configure<ProductImageStorageOptions>(config.GetSection(ProductImageStorageOptions.SectionName));
        services.AddSingleton<IProductImageProcessor, MagickProductImageProcessor>();
        services.AddSingleton<IProductImageObjectStore, LocalFileProductImageStore>();
        services.AddScoped<ICatalogImportJobRepository, CatalogImportJobRepository>();
        services.AddScoped<ISaleRepository, SaleRepository>();
        services.AddScoped<ISaleMutationLock, PosSaleMutationLock>();
        services.AddScoped<ICustomerOrderRepository, CustomerOrderRepository>();
        services.AddScoped<ICustomerOrderStockService, CustomerOrderStockService>();
        services.AddScoped<ISaleReturnRepository, SaleReturnRepository>();
        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IInventoryLotRepository, InventoryLotRepository>();
        services.AddScoped<ExItS.PinoyBusinessPOS.Application.Reporting.IManagementOverviewReadStore, ManagementOverviewReadStore>();
        services.AddScoped<InventoryLotStockService>();
        services.AddScoped<IInventoryReorderChangeRepository, InventoryReorderChangeRepository>();
        services.AddScoped<IInventoryTransferRepository, InventoryTransferRepository>();
        services.AddScoped<IInventoryBranchBalanceRepository, InventoryBranchBalanceRepository>();
        services.AddSingleton<IInventoryTransferAlertSink, NoOpInventoryTransferAlertSink>();
        services.AddScoped<IDirectPurchaseReceiptRepository, DirectPurchaseReceiptRepository>();
        services.AddScoped<IStockCountRepository, StockCountRepository>();
        services.AddScoped<ICashierShiftRepository, CashierShiftRepository>();
        services.AddScoped<IPosRoleAssignmentRepository, PosRoleAssignmentRepository>();
        services.AddScoped<IExpenseCategoryRepository, ExpenseCategoryRepository>();
        services.AddScoped<IExpenseRepository, ExpenseRepository>();
        services.AddScoped<ISupplierRepository, SupplierRepository>();
        services.AddScoped<IConnectedSupplierRelationshipRepository, ConnectedSupplierRelationshipRepository>();
        services.AddScoped<ISupplierProductExposureRepository, SupplierProductExposureRepository>();
        services.AddScoped<IConnectedBuyerProductShareRepository, ConnectedBuyerProductShareRepository>();
        services.AddScoped<IBuyerSupplierProductLinkRepository, BuyerSupplierProductLinkRepository>();
        services.AddScoped<IConnectedPurchaseOrderRepository, ConnectedPurchaseOrderRepository>();
        services.AddScoped<IRegisterRepository, RegisterRepository>();
        services.AddScoped<IPosOperationalSetupRepository, OperationalSetupRepository>();
        services.AddScoped<IOrganizationCashDenominationRepository, OrganizationCashDenominationRepository>();
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
