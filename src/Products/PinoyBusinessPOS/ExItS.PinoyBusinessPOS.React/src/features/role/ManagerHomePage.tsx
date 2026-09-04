import { isWarehouseBranch } from "@/features/branches/branch-type";
import { ManagerRetailHome } from "@/features/role/ManagerRetailHome";
import { ManagerWarehouseHome } from "@/features/role/ManagerWarehouseHome";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

/**
 * Operations Manager Home — daily command center (not a navigation directory).
 * Retail vs Warehouse composition follows the bound branch type.
 */
export function ManagerHomePage() {
  const { boundWorkspace } = useWorkspace();
  const warehouse = isWarehouseBranch(boundWorkspace?.branchType);

  if (warehouse) {
    return <ManagerWarehouseHome homeTestId="manager-home" />;
  }

  return <ManagerRetailHome />;
}
