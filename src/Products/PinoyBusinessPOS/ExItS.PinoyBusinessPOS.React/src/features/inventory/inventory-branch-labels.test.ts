import { describe, expect, it } from "vitest";
import {
  buildBranchNameById,
  resolveInventoryBranchDisplayName,
} from "@/features/inventory/inventory-branch-labels";

describe("inventory branch display names", () => {
  const kaliboId = "56A8A186-1111-1111-1111-111111111111";
  const iloiloId = "C3CD1C39-2222-2222-2222-222222222222";

  it("resolves names from workspace directory (case-insensitive)", () => {
    const map = buildBranchNameById([
      { branchId: kaliboId.toLowerCase(), name: "Kalibo Branch" },
      { branchId: iloiloId.toLowerCase(), name: "Iloilo Branch" },
    ]);
    expect(
      resolveInventoryBranchDisplayName({
        branchId: kaliboId,
        branchNameById: map,
      }),
    ).toBe("Kalibo Branch");
    expect(
      resolveInventoryBranchDisplayName({
        branchId: iloiloId,
        branchNameById: map,
      }),
    ).toBe("Iloilo Branch");
  });

  it("prefers current workspace branch name when ids match ignoring case", () => {
    expect(
      resolveInventoryBranchDisplayName({
        branchId: kaliboId.toLowerCase(),
        branchNameById: new Map(),
        currentBranchId: kaliboId,
        currentBranchName: "Kalibo Branch",
      }),
    ).toBe("Kalibo Branch");
  });

  it("does not show truncated guid when name is missing", () => {
    const label = resolveInventoryBranchDisplayName({
      branchId: kaliboId,
      branchNameById: new Map(),
      unknownLabel: "Unknown branch",
    });
    expect(label).toBe("Unknown branch");
    expect(label).not.toContain("56a8a186");
  });
});
