import { describe, expect, it } from "vitest";
import {
  groupWorkspaceBranchesByArea,
  resolveWorkspaceBranchGroupingMode,
  summarizeWorkspaceLocations,
} from "@/features/workspace/workspace-area-grouping";
import type { AccessibleWorkspaceBranch } from "@/workspace/types";

function branch(
  branchId: string,
  name: string,
  areaId: string | null = null,
  areaName: string | null = null,
  branchType: AccessibleWorkspaceBranch["branchType"] = "Retail",
): AccessibleWorkspaceBranch {
  return {
    branchId,
    name,
    secondaryLine: "",
    isPrimary: false,
    isActive: true,
    areaId,
    areaName,
    branchType,
  };
}

describe("workspace area grouping", () => {
  it("AREA02-01 keeps the simple flow for a single branch without areas", () => {
    expect(resolveWorkspaceBranchGroupingMode([branch("b1", "Main")])).toBe("single");
    expect(resolveWorkspaceBranchGroupingMode([])).toBe("single");
  });

  it("AREA02-02 keeps the flat list for several branches without areas", () => {
    const mode = resolveWorkspaceBranchGroupingMode([
      branch("b1", "Main"),
      branch("b2", "Iloilo"),
      branch("b3", "Manila"),
    ]);

    expect(mode).toBe("flat");
  });

  it("AREA02-03 groups by area and orders unassigned last", () => {
    const groups = groupWorkspaceBranchesByArea([
      branch("b4", "Manila"),
      branch("b3", "Cebu", "area-visayas", "VISAYAS"),
      branch("b1", "Main", "area-panay", "PANAY"),
      branch("b2", "Iloilo", "area-panay", "PANAY"),
    ]);

    expect(resolveWorkspaceBranchGroupingMode([branch("b1", "Main", "area-panay", "PANAY")])).toBe(
      "grouped",
    );
    expect(groups.map((group) => group.areaName)).toEqual(["PANAY", "VISAYAS", null]);
    expect(groups[0].branches.map((b) => b.name)).toEqual(["Main", "Iloilo"]);
    expect(groups[2].isUnassigned).toBe(true);
    expect(groups[2].key).toBe("unassigned");
  });

  it("AREA02-04 omits the unassigned group when every visible branch has an area", () => {
    const groups = groupWorkspaceBranchesByArea([
      branch("b1", "Main", "area-panay", "PANAY"),
      branch("b3", "Cebu", "area-visayas", "VISAYAS"),
    ]);

    expect(groups).toHaveLength(2);
    expect(groups.some((group) => group.isUnassigned)).toBe(false);
  });

  it("AREA02-04 groups only the branches it was given, so access filtering still decides visibility", () => {
    const authorized = [branch("b1", "Main", "area-panay", "PANAY")];

    const groups = groupWorkspaceBranchesByArea(authorized);

    expect(groups.flatMap((group) => group.branches.map((b) => b.branchId))).toEqual(["b1"]);
    expect(groups[0].branches).toHaveLength(1);
  });

  it("recovers an area label from any branch in the group", () => {
    const groups = groupWorkspaceBranchesByArea([
      branch("b1", "Main", "area-panay", null),
      branch("b2", "Iloilo", "area-panay", "PANAY"),
    ]);

    expect(groups[0].areaName).toBe("PANAY");
  });

  it("summarizes retail and warehouse counts for area headings", () => {
    const breakdown = summarizeWorkspaceLocations([
      branch("b1", "Main", "a1", "North", "Retail"),
      branch("b2", "Side", "a1", "North", "Retail"),
      branch("w1", "WH", "a1", "North", "Warehouse"),
    ]);
    expect(breakdown).toEqual({ total: 3, retail: 2, warehouse: 1 });
  });
});
