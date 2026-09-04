import { ManagerWarehouseHome } from "@/features/role/ManagerWarehouseHome";

/**
 * Warehouse branch home — same command-center composition as Manager Warehouse Home.
 * Route: /warehouse (Operations warehouse Home tab).
 */
export function WarehouseDashboardPage() {
  return <ManagerWarehouseHome enforceWarehouseBranch homeTestId="warehouse-dashboard" />;
}
