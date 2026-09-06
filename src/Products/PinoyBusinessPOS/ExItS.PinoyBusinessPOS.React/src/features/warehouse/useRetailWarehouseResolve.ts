import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { listSupplyRoutesByDestination } from "@/api/pos/pos-supply-routes-client";
import {
  resolveRetailWarehouseSupply,
  type RetailWarehouseResolveState,
} from "@/features/warehouse/retail-warehouse-resolve";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function useRetailWarehouseResolve() {
  const { boundWorkspace, workspaces } = useWorkspace();

  const workspace = useMemo(
    () =>
      boundWorkspace?.branchId
        ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
        : null,
    [boundWorkspace],
  );

  const orgBranches = useMemo(() => {
    const org = workspaces.find((w) => w.organizationId === boundWorkspace?.organizationId);
    return org?.branches ?? [];
  }, [workspaces, boundWorkspace?.organizationId]);

  const routesQuery = useQuery({
    queryKey: ["supply-routes-dest", workspace?.organizationId, workspace?.branchId],
    enabled: Boolean(workspace?.branchId),
    queryFn: ({ signal }) => listSupplyRoutesByDestination(workspace!, workspace!.branchId!, signal),
  });

  const resolveState: RetailWarehouseResolveState | null = useMemo(() => {
    if (!workspace?.branchId) return null;
    if (routesQuery.isPending) return null;
    return resolveRetailWarehouseSupply(
      orgBranches,
      routesQuery.data ?? [],
      workspace.branchId,
    );
  }, [workspace?.branchId, orgBranches, routesQuery.data, routesQuery.isPending]);

  return {
    workspace,
    boundWorkspace,
    orgBranches,
    routesQuery,
    resolveState,
    isLoading: Boolean(workspace?.branchId) && routesQuery.isPending,
    isError: routesQuery.isError,
  };
}
