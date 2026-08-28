using ExItS.PinoyBusinessPOS.Application.Common;
using ExItS.PinoyBusinessPOS.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace ExItS.PinoyBusinessPOS.Infrastructure.Persistence;

internal static class PersistenceExceptionMapper
{
    public static bool TryMapUniqueViolation(DbUpdateException exception, out string errorCode, out string message)
    {
        errorCode = ApplicationErrorCodes.DomainViolation;
        message = "A persistence constraint was violated.";

        if (exception.InnerException is not PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } pg)
        {
            return false;
        }

        var constraint = pg.ConstraintName ?? string.Empty;
        if (constraint.Contains("ux_products_org_normalized_sku", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.ProductSkuConflict;
            message = "This SKU is already used by another product in this organization, including inactive products.";
            return true;
        }

        if (constraint.Contains("ux_products_org_barcode", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.ProductBarcodeConflict;
            message = "This barcode is already used by another product in this organization, including inactive products.";
            return true;
        }

        if (constraint.Contains("ux_product_categories_org_active_name", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.CategoryNameConflict;
            message = "An active category with this name already exists in this organization.";
            return true;
        }

        if (constraint.Contains("ux_product_brands_org_active_name", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.BrandNameConflict;
            message = "An active brand with this name already exists in this organization.";
            return true;
        }

        if (constraint.Contains("ux_sales_org_sale_number", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.SaleNumberConflict;
            message = "A sale number was allocated concurrently. Retry the checkout.";
            return true;
        }

        if (constraint.Contains("ux_suppliers_org_active_name", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.SupplierNameConflict;
            message = "An active supplier with this name already exists in this organization.";
            return true;
        }

        if (constraint.Contains("ux_suppliers_org_supplier_code", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.SupplierCodeConflict;
            message = "A supplier code was allocated concurrently. Retry the create.";
            return true;
        }

        if (constraint.Contains("ux_registers_org_normalized_name", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.RegisterNameConflict;
            message = "A register with this name already exists in this organization.";
            return true;
        }

        if (constraint.Contains("ux_registers_org_register_code", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.RegisterCodeConflict;
            message = "A register code was allocated concurrently. Retry the create.";
            return true;
        }

        if (constraint.Contains("ux_expense_categories_org_active_name", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.ExpenseCategoryNameConflict;
            message = "An active expense category with this name already exists in this organization.";
            return true;
        }

        if (constraint.Contains("ux_customers_org_active_mobile", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.MobileConflict;
            message = "An active customer with this mobile number already exists in this organization.";
            return true;
        }

        if (constraint.Contains("ux_customers_org_platform_business_customer", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.PlatformBusinessCustomerCorrelationConflict;
            message = "Another POS customer in this organization is already correlated to that Platform BusinessCustomer.";
            return true;
        }

        if (constraint.Contains("ux_customers_org_linked_personal", StringComparison.OrdinalIgnoreCase)
            || constraint.Contains("ux_customers_org_linked_buyer_org", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = DomainErrorCodes.CustomerExItsIdentityLinkConflict;
            message = "Another POS customer in this organization is already linked to that ExItS identity.";
            return true;
        }

        if (constraint.Contains("ux_inventory_transfers_org_transfer_number", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.InventoryTransferNumberConflict;
            message = "A transfer number was allocated concurrently. Retry the dispatch.";
            return true;
        }

        if (constraint.Contains("ux_stock_movements_inventory_transfer_source", StringComparison.OrdinalIgnoreCase))
        {
            errorCode = ApplicationErrorCodes.InventoryTransferAlreadyReceived;
            message = "This transfer stock movement has already been applied.";
            return true;
        }

        return true;
    }
}
