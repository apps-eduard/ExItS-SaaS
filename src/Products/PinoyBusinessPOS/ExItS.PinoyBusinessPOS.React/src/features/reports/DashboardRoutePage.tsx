import { isWarehouseBranch } from "@/features/branches/branch-type";
import { ManagementDashboardPage } from "@/features/reports/ManagementDashboardPage";
import { WarehouseManagementDashboardPage } from "@/features/reports/WarehouseManagementDashboardPage";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Public /dashboard route: Retail keeps sales/business dashboard;
 * Warehouse gets operations analytics (no sell/payment metrics).
 */
export function DashboardRoutePage() {
  const { boundWorkspace } = useWorkspace();
  if (isWarehouseBranch(boundWorkspace?.branchType)) {
    return <WarehouseManagementDashboardPage />;
  }
  return <ManagementDashboardPage />;
}
