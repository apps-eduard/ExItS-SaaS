import { describe, expect, it } from "vitest";
import { resolveRetailWarehouseNavigation } from "@/features/warehouse/retail-warehouse-gate";
import type { MessageKey } from "@/i18n/messages";

const t = (key: MessageKey) => key;

describe("resolveRetailWarehouseNavigation", () => {
  it("navigates when ready", () => {
    expect(
      resolveRetailWarehouseNavigation(
        {
          kind: "ready",
          supplyWarehouseId: "w",
          supplyWarehouseName: "WH",
          isPreferred: true,
        },
        { canManageOrganization: false, canInvite: false, canManageInventory: false },
        t,
      ),
    ).toEqual({ kind: "navigate", to: "/warehouse" });
  });

  it("toasts no-warehouse with branch admin action when allowed", () => {
    const result = resolveRetailWarehouseNavigation(
      { kind: "no-warehouse" },
      { canManageOrganization: true, canInvite: false, canManageInventory: false },
      t,
    );
    expect(result.kind).toBe("toast");
    if (result.kind !== "toast") return;
    expect(result.toast.title).toBe("warehouse.toast.noWarehouse.title");
    expect(result.toast.action).toEqual({
      label: "warehouse.toast.noWarehouse.action",
      href: "/org/branches",
    });
  });

  it("toasts no-assignment with configure action for inventory managers", () => {
    const tWithBranch = (key: MessageKey) =>
      key === "warehouse.toast.noAssignment.description"
        ? "{branch} has no supply warehouse route."
        : key;
    const result = resolveRetailWarehouseNavigation(
      { kind: "no-assignment" },
      { canManageOrganization: false, canInvite: false, canManageInventory: true },
      tWithBranch,
      { branchName: "Pac Passi" },
    );
    expect(result.kind).toBe("toast");
    if (result.kind !== "toast") return;
    expect(result.toast.description).toContain("Pac Passi");
    expect(result.toast.action).toEqual({
      label: "warehouse.toast.noAssignment.action",
      href: "/org/supply-routes",
    });
  });
});
