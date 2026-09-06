import { Navigate } from "react-router-dom";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { WarehouseDashboardPage } from "@/features/warehouse/WarehouseDashboardPage";
import { RetailWarehouseShell } from "@/features/warehouse/RetailWarehouseShell";
import { RetailWarehouseOverviewPage } from "@/features/warehouse/RetailWarehouseOverviewPage";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * `/warehouse` index — Warehouse branch home must not regress;
 * Retail opens the retail warehouse overview inside the workspace shell.
 */
export function WarehouseIndexPage() {
  const { boundWorkspace } = useWorkspace();
  if (isWarehouseBranch(boundWorkspace?.branchType)) {
    return <WarehouseDashboardPage />;
  }
  if (!boundWorkspace?.branchId) {
    return <Navigate to="/workspace" replace />;
  }
  return (
    <RetailWarehouseShell>
      <RetailWarehouseOverviewPage />
    </RetailWarehouseShell>
  );
}
