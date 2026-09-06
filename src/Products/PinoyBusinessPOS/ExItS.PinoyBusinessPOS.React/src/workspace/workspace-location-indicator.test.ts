import { describe, expect, it } from "vitest";
import {
  buildWorkspaceLocationSecondary,
  isWorkspaceChooserPath,
  resolveWorkspaceLocationIndicator,
} from "@/workspace/workspace-location-indicator";
import type { BoundWorkspace } from "@/workspace/types";

function bound(partial: Partial<BoundWorkspace> & Pick<BoundWorkspace, "branchId" | "branchName">): BoundWorkspace {
  return {
    organizationId: "org-1",
    organizationDisplayName: "Kizy Store",
    branchType: "Retail",
    experience: "operations",
    areaId: null,
    areaName: null,
    ...partial,
  };
}

describe("workspace-location-indicator", () => {
  it("shows warehouse location primary with area and type secondary", () => {
    const model = resolveWorkspaceLocationIndicator({
      boundWorkspace: bound({
        branchId: "w1",
        branchName: "Panay Warehouse",
        branchType: "Warehouse",
        areaId: "a1",
        areaName: "Pacifica Nort Area",
      }),
      chooseWorkspaceLabel: "Choose workspace",
      retailLabel: "Retail",
      warehouseLabel: "Warehouse",
    });
    expect(model.primary).toBe("Panay Warehouse");
    expect(model.secondary).toBe("Pacifica Nort Area · Warehouse");
    expect(model.primary).not.toBe("Pacifica Nort Area");
    expect(model.hasBoundLocation).toBe(true);
  });

  it("shows retail location with area secondary", () => {
    const model = resolveWorkspaceLocationIndicator({
      boundWorkspace: bound({
        branchId: "b1",
        branchName: "Main Branch",
        branchType: "Retail",
        areaName: "Pasi Norte",
      }),
      chooseWorkspaceLabel: "Choose workspace",
      retailLabel: "Retail",
      warehouseLabel: "Warehouse",
    });
    expect(model.primary).toBe("Main Branch");
    expect(model.secondary).toBe("Pasi Norte · Retail");
  });

  it("shows type only when no area", () => {
    const model = resolveWorkspaceLocationIndicator({
      boundWorkspace: bound({
        branchId: "b1",
        branchName: "Main Branch",
        branchType: "Retail",
      }),
      chooseWorkspaceLabel: "Choose workspace",
      retailLabel: "Retail",
      warehouseLabel: "Warehouse",
    });
    expect(model.primary).toBe("Main Branch");
    expect(model.secondary).toBe("Retail");
  });

  it("shows choose workspace when unbound or no location", () => {
    expect(
      resolveWorkspaceLocationIndicator({
        boundWorkspace: null,
        chooseWorkspaceLabel: "Choose workspace",
        retailLabel: "Retail",
        warehouseLabel: "Warehouse",
      }).primary,
    ).toBe("Choose workspace");
    expect(
      resolveWorkspaceLocationIndicator({
        boundWorkspace: bound({
          branchId: null as unknown as string,
          branchName: null as unknown as string,
          experience: "manage_business",
        }),
        chooseWorkspaceLabel: "Choose workspace",
        retailLabel: "Retail",
        warehouseLabel: "Warehouse",
      }).hasBoundLocation,
    ).toBe(false);
  });

  it("never uses area as primary even if org name looks like an area", () => {
    const model = resolveWorkspaceLocationIndicator({
      boundWorkspace: bound({
        organizationDisplayName: "Pacifica Nort Area",
        branchId: "w1",
        branchName: "Panay Warehouse",
        branchType: "Warehouse",
        areaName: "Pacifica Nort Area",
      }),
      chooseWorkspaceLabel: "Choose workspace",
      retailLabel: "Retail",
      warehouseLabel: "Warehouse",
    });
    expect(model.primary).toBe("Panay Warehouse");
    expect(model.secondary?.startsWith("Pacifica Nort Area")).toBe(true);
  });

  it("builds secondary lines correctly", () => {
    expect(
      buildWorkspaceLocationSecondary({
        areaName: "Pasi Norte",
        typeLabel: "Retail",
        retailLabel: "Retail",
        warehouseLabel: "Warehouse",
      }),
    ).toBe("Pasi Norte · Retail");
    expect(
      buildWorkspaceLocationSecondary({
        areaName: null,
        typeLabel: "Warehouse",
        retailLabel: "Retail",
        warehouseLabel: "Warehouse",
      }),
    ).toBe("Warehouse");
  });

  it("detects workspace chooser paths for no-op navigation", () => {
    expect(isWorkspaceChooserPath("/workspace")).toBe(true);
    expect(isWorkspaceChooserPath("/workspace/")).toBe(true);
    expect(isWorkspaceChooserPath("/warehouse")).toBe(false);
    expect(isWorkspaceChooserPath("/inventory")).toBe(false);
  });
});
