import type { ToastPayload } from "@/components/exits/ToastProvider";
import type { MessageKey } from "@/i18n/messages";
import type { RetailWarehouseResolveState } from "@/features/warehouse/retail-warehouse-resolve";

export type RetailWarehouseGateCapabilities = {
  canManageOrganization: boolean;
  canInvite: boolean;
  canManageInventory: boolean;
};

export type RetailWarehouseGateResult =
  | { kind: "navigate"; to: string }
  | { kind: "toast"; toast: ToastPayload };

type Translate = (key: MessageKey, vars?: Record<string, string | number>) => string;

/**
 * Pure gate used by Warehouse quick access / inventory entry points.
 */
export function resolveRetailWarehouseNavigation(
  state: RetailWarehouseResolveState,
  caps: RetailWarehouseGateCapabilities,
  t: Translate,
  options?: { branchName?: string; warehousePath?: string },
): RetailWarehouseGateResult {
  const warehousePath = options?.warehousePath ?? "/warehouse";

  if (state.kind === "ready") {
    return { kind: "navigate", to: warehousePath };
  }

  if (state.kind === "no-warehouse") {
    const canConfigureBranches = caps.canManageOrganization || caps.canInvite;
    return {
      kind: "toast",
      toast: {
        title: t("warehouse.toast.noWarehouse.title"),
        description: canConfigureBranches
          ? t("warehouse.toast.noWarehouse.description")
          : t("warehouse.toast.noWarehouse.askAdmin"),
        tone: "error",
        action: canConfigureBranches
          ? {
              label: t("warehouse.toast.noWarehouse.action"),
              href: "/org/branches",
            }
          : undefined,
      },
    };
  }

  const branchLabel = options?.branchName?.trim() || t("retailWarehouse.thisBranch");
  return {
    kind: "toast",
    toast: {
      title: t("warehouse.toast.noAssignment.title"),
      description: caps.canManageInventory
        ? t("warehouse.toast.noAssignment.description").replace("{branch}", branchLabel)
        : t("warehouse.toast.noAssignment.askManager").replace("{branch}", branchLabel),
      tone: "error",
      action: caps.canManageInventory
        ? {
            label: t("warehouse.toast.noAssignment.action"),
            href: "/org/supply-routes",
          }
        : undefined,
    },
  };
}
