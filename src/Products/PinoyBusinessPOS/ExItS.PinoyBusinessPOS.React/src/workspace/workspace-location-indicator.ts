import { isWarehouseBranch } from "@/features/branches/branch-type";
import type { AccessibleOrganizationWorkspace, BoundWorkspace } from "@/workspace/types";

export type WorkspaceLocationTypeLabel = "Retail" | "Warehouse";

export type WorkspaceLocationIndicatorInput = {
  boundWorkspace: BoundWorkspace | null;
  workspaces?: ReadonlyArray<AccessibleOrganizationWorkspace>;
  chooseWorkspaceLabel: string;
  retailLabel: string;
  warehouseLabel: string;
};

export type WorkspaceLocationIndicatorModel = {
  primary: string;
  /** Area · Type, Type alone, or null when unbound / no type. */
  secondary: string | null;
  areaName: string | null;
  typeLabel: WorkspaceLocationTypeLabel | null;
  hasBoundLocation: boolean;
  title: string;
  /** Details after the switch verb, for aria-label composition. */
  detailsForAria: string;
};

function resolveAreaAndType(
  bound: BoundWorkspace,
  workspaces: ReadonlyArray<AccessibleOrganizationWorkspace> | undefined,
): { areaName: string | null; typeLabel: WorkspaceLocationTypeLabel | null } {
  let areaName = bound.areaName ?? null;
  let branchType = bound.branchType ?? null;

  if (bound.branchId && workspaces?.length) {
    const org = workspaces.find((w) => w.organizationId === bound.organizationId);
    const branch = org?.branches.find((b) => b.branchId === bound.branchId);
    if (branch) {
      if (!areaName) areaName = branch.areaName ?? null;
      if (!branchType) branchType = branch.branchType ?? null;
    }
  }

  const typeLabel: WorkspaceLocationTypeLabel | null = branchType
    ? isWarehouseBranch(branchType)
      ? "Warehouse"
      : "Retail"
    : null;

  return { areaName, typeLabel };
}

export function buildWorkspaceLocationSecondary(input: {
  areaName: string | null;
  typeLabel: WorkspaceLocationTypeLabel | null;
  retailLabel: string;
  warehouseLabel: string;
}): string | null {
  const typeText =
    input.typeLabel === "Warehouse"
      ? input.warehouseLabel
      : input.typeLabel === "Retail"
        ? input.retailLabel
        : null;
  const area = input.areaName?.trim() || null;
  if (area && typeText) return `${area} · ${typeText}`;
  if (area) return area;
  if (typeText) return typeText;
  return null;
}

/**
 * Location-first topbar model. Area is never primary. Organization name is never primary
 * when a branch location is bound.
 */
export function resolveWorkspaceLocationIndicator(
  input: WorkspaceLocationIndicatorInput,
): WorkspaceLocationIndicatorModel {
  const bound = input.boundWorkspace;

  if (!bound?.branchId || !bound.branchName) {
    const primary = input.chooseWorkspaceLabel;
    return {
      primary,
      secondary: null,
      areaName: null,
      typeLabel: null,
      hasBoundLocation: false,
      title: primary,
      detailsForAria: primary,
    };
  }

  const { areaName, typeLabel } = resolveAreaAndType(bound, input.workspaces);
  const secondary = buildWorkspaceLocationSecondary({
    areaName,
    typeLabel,
    retailLabel: input.retailLabel,
    warehouseLabel: input.warehouseLabel,
  });
  const primary = bound.branchName;
  const detailsParts = [primary];
  if (typeLabel) {
    detailsParts.push(typeLabel === "Warehouse" ? input.warehouseLabel : input.retailLabel);
  }
  if (areaName?.trim()) detailsParts.push(areaName.trim());

  return {
    primary,
    secondary,
    areaName: areaName?.trim() || null,
    typeLabel,
    hasBoundLocation: true,
    title: secondary ? `${primary} — ${secondary}` : primary,
    detailsForAria: detailsParts.join(", "),
  };
}

export function isWorkspaceChooserPath(pathname: string): boolean {
  return pathname === "/workspace" || pathname.startsWith("/workspace/");
}
