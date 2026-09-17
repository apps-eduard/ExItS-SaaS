import { describe, expect, it } from "vitest";
import {
  applyVisibilityMode,
  areaCheckboxState,
  areaSelectedCount,
  diffBranchAccess,
  groupBranchesByArea,
  resolveVisibilityMode,
  toggleAreaSelection,
  toggleBranchSelection,
  withHomeBranchLocked,
  type VisibilityBranch,
} from "@/features/customers/customer-branch-visibility";

const iloilo: VisibilityBranch = {
  id: "b-iloilo",
  name: "Iloilo Main",
  areaId: "a-wv",
  areaName: "Western Visayas",
  status: "Active",
};
const kalibo: VisibilityBranch = {
  id: "b-kalibo",
  name: "Kalibo",
  areaId: "a-wv",
  areaName: "Western Visayas",
  status: "Active",
};
const roxas: VisibilityBranch = {
  id: "b-roxas",
  name: "Roxas",
  areaId: "a-wv",
  areaName: "Western Visayas",
  status: "Active",
};
const manila: VisibilityBranch = {
  id: "b-manila",
  name: "Manila Main",
  areaId: "a-mm",
  areaName: "Metro Manila",
  status: "Active",
};
const unassigned: VisibilityBranch = {
  id: "b-other",
  name: "Other Shop",
  areaId: null,
  areaName: null,
  status: "Active",
};

const areas = [
  { id: "a-wv", name: "Western Visayas" },
  { id: "a-mm", name: "Metro Manila" },
];

describe("customer-branch-visibility area helpers", () => {
  it("groups by Area and keeps unassigned selectable", () => {
    const groups = groupBranchesByArea(
      [iloilo, kalibo, roxas, manila, unassigned],
      areas,
      "Unassigned branches",
    );
    expect(groups).toHaveLength(3);
    expect(groups[0]?.name).toBe("Western Visayas");
    expect(groups[0]?.branches.map((b) => b.name)).toEqual([
      "Iloilo Main",
      "Kalibo",
      "Roxas",
    ]);
    expect(groups[1]?.name).toBe("Metro Manila");
    expect(groups[2]?.name).toBe("Unassigned branches");
    expect(groups[2]?.branches).toHaveLength(1);
  });

  it("falls back to a flat single group of branches when Areas are absent", () => {
    const groups = groupBranchesByArea(
      [iloilo, kalibo, unassigned],
      [],
      "Unassigned branches",
    );
    expect(groups).toHaveLength(1);
    expect(groups[0]?.areaId).toBeNull();
    expect(groups[0]?.branches).toHaveLength(3);
  });

  it("checking an Area selects all of its branches", () => {
    const group = groupBranchesByArea([iloilo, kalibo, roxas, manila], areas, "Unassigned")[0]!;
    const next = toggleAreaSelection({
      group,
      selected: new Set(["b-iloilo"]),
      homeBranchId: "b-iloilo",
    });
    expect([...next].sort()).toEqual(["b-iloilo", "b-kalibo", "b-roxas"]);
    expect(areaCheckboxState(group, next)).toBe("checked");
  });

  it("unchecking one branch makes the Area indeterminate", () => {
    const group = groupBranchesByArea([iloilo, kalibo, roxas], areas, "Unassigned")[0]!;
    let selected = new Set(["b-iloilo", "b-kalibo", "b-roxas"]);
    selected = toggleBranchSelection({
      branchId: "b-kalibo",
      selected,
      homeBranchId: "b-iloilo",
    });
    expect(selected.has("b-kalibo")).toBe(false);
    expect(areaCheckboxState(group, selected)).toBe("indeterminate");
    expect(areaSelectedCount(group, selected)).toEqual({ selected: 2, total: 3 });
  });

  it("rechecking an indeterminate Area selects all again", () => {
    const group = groupBranchesByArea([iloilo, kalibo, roxas], areas, "Unassigned")[0]!;
    const selected = toggleAreaSelection({
      group,
      selected: new Set(["b-iloilo", "b-roxas"]),
      homeBranchId: "b-iloilo",
    });
    expect(areaCheckboxState(group, selected)).toBe("checked");
    expect([...selected].sort()).toEqual(["b-iloilo", "b-kalibo", "b-roxas"]);
  });

  it("unchecking a fully selected Area keeps the locked Home branch", () => {
    const group = groupBranchesByArea([iloilo, kalibo, roxas], areas, "Unassigned")[0]!;
    const selected = toggleAreaSelection({
      group,
      selected: new Set(["b-iloilo", "b-kalibo", "b-roxas"]),
      homeBranchId: "b-iloilo",
    });
    expect([...selected]).toEqual(["b-iloilo"]);
    expect(areaCheckboxState(group, selected)).toBe("indeterminate");
  });

  it("cannot uncheck the Home branch", () => {
    const selected = toggleBranchSelection({
      branchId: "b-iloilo",
      selected: new Set(["b-iloilo", "b-kalibo"]),
      homeBranchId: "b-iloilo",
    });
    expect(selected.has("b-iloilo")).toBe(true);
    expect(withHomeBranchLocked([], "b-iloilo").has("b-iloilo")).toBe(true);
  });

  it("leaves branches outside the Area unaffected", () => {
    const wv = groupBranchesByArea([iloilo, kalibo, roxas, manila], areas, "Unassigned")[0]!;
    const selected = toggleAreaSelection({
      group: wv,
      selected: new Set(["b-iloilo", "b-manila"]),
      homeBranchId: "b-iloilo",
    });
    expect(selected.has("b-manila")).toBe(true);
    expect(selected.has("b-kalibo")).toBe(true);
  });

  it("keeps unassigned branches selectable independently", () => {
    const groups = groupBranchesByArea([iloilo, unassigned], areas, "Unassigned branches");
    const other = groups.find((g) => g.areaId === null)!;
    const selected = toggleBranchSelection({
      branchId: "b-other",
      selected: new Set(["b-iloilo"]),
      homeBranchId: "b-iloilo",
    });
    expect(selected.has("b-other")).toBe(true);
    expect(areaCheckboxState(other, selected)).toBe("checked");
  });

  it("All branches expands to every eligible id; mode resolves back after customize", () => {
    const eligible = ["b-iloilo", "b-kalibo", "b-roxas", "b-manila"];
    const all = applyVisibilityMode("all", eligible, "b-iloilo");
    expect(resolveVisibilityMode(all, eligible, "b-iloilo")).toBe("all");
    const customized = toggleBranchSelection({
      branchId: "b-manila",
      selected: all,
      homeBranchId: "b-iloilo",
    });
    expect(resolveVisibilityMode(customized, eligible, "b-iloilo")).toBe("selected");
    expect(resolveVisibilityMode(new Set(["b-iloilo"]), eligible, "b-iloilo")).toBe(
      "this_branch",
    );
  });

  it("diff saves explicit branch ids and never auto-shares a later new branch", () => {
    const { toGrant, toRevoke } = diffBranchAccess({
      currentBranchIds: ["b-iloilo", "b-kalibo"],
      desiredBranchIds: new Set(["b-iloilo", "b-roxas"]),
      homeBranchId: "b-iloilo",
    });
    expect(toGrant).toEqual(["b-roxas"]);
    expect(toRevoke).toEqual(["b-kalibo"]);

    // A newly created branch id is absent from desired → not granted.
    const later = diffBranchAccess({
      currentBranchIds: ["b-iloilo", "b-roxas"],
      desiredBranchIds: new Set(["b-iloilo", "b-roxas"]),
      homeBranchId: "b-iloilo",
    });
    expect(later.toGrant).toEqual([]);
    expect(later.toRevoke).toEqual([]);
  });
});
