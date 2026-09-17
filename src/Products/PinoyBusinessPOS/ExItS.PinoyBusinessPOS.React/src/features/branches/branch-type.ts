/** Platform / POS organization branch type (Retail default). */
export type OrganizationBranchType = "Retail" | "Warehouse";

export const ORGANIZATION_BRANCH_TYPES = ["Retail", "Warehouse"] as const;

export function normalizeBranchType(value: unknown): OrganizationBranchType {
  if (typeof value === "string" && value.trim().toLowerCase() === "warehouse") {
    return "Warehouse";
  }
  return "Retail";
}

export function isWarehouseBranch(
  branchType: OrganizationBranchType | string | null | undefined,
): boolean {
  return normalizeBranchType(branchType) === "Warehouse";
}
