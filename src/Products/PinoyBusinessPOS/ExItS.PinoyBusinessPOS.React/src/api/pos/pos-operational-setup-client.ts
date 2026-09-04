import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { posRequest } from "@/api/pos/pos-http";

const SETUP_PATH = "/api/v1/pos/operational-setup";
const DENOMINATIONS_PATH = `${SETUP_PATH}/cash-denominations`;

/** Matches PhilippineCashDenominationDefaults.Values in the POS API. */
export const DEFAULT_PHP_CASH_DENOMINATION_VALUES = [
  1000, 500, 200, 100, 50, 20, 10, 5, 1, 0.25, 0.1, 0.05,
] as const;

export type PosOperationalSetupDto = {
  organizationId: string;
  storeDisplayName: string;
  currencyCode: string;
  taxPricingMode: string;
  taxRatePercent: number;
  receiptHeader?: string | null;
  receiptFooter?: string | null;
  businessAddress?: string | null;
  contactPhone?: string | null;
  defaultRegisterId?: string | null;
  cashCountMode: string;
  openingCashCountMode?: string | null;
  closingCashCountMode?: string | null;
  isComplete: boolean;
  isCompleted?: boolean;
  completedAtUtc?: string | null;
  createdAtUtc: string;
  createdBy: string;
  updatedAtUtc: string;
  updatedBy: string;
  taxConfigurationEnabled?: boolean;
};

export type OrganizationCashDenominationDto = {
  denominationId: string;
  organizationId: string;
  value: number;
  displayLabel?: string | null;
  isEnabled: boolean;
  sortOrder: number;
  updatedAtUtc: string;
};

export type CashDenominationWriteDto = {
  value: number;
  isEnabled?: boolean;
  sortOrder?: number;
  displayLabel?: string | null;
  denominationId?: string | null;
};

export type UpdateOperationalSetupRequest = {
  storeDisplayName: string;
  currencyCode: string;
  taxPricingMode: string;
  taxRatePercent: number;
  expectedUpdatedAtUtc: string;
  receiptHeader?: string | null;
  receiptFooter?: string | null;
  businessAddress?: string | null;
  contactPhone?: string | null;
  cashCountMode?: string | null;
  openingCashCountMode?: string | null;
  closingCashCountMode?: string | null;
};

export type ReplaceCashDenominationsRequest = {
  items: CashDenominationWriteDto[];
};

/** Cash count policy for open/close shift — ViewOperationalSetup. */
export function getOperationalSetup(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<PosOperationalSetupDto> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: SETUP_PATH,
  });
}

export function updateOperationalSetup(
  workspace: PosWorkspaceScope,
  body: UpdateOperationalSetupRequest,
  signal?: AbortSignal,
): Promise<PosOperationalSetupDto> {
  return posRequest({
    method: "PUT",
    workspace,
    signal,
    path: SETUP_PATH,
    body,
  });
}

export function listCashDenominations(
  workspace: PosWorkspaceScope,
  signal?: AbortSignal,
): Promise<OrganizationCashDenominationDto[]> {
  return posRequest({
    method: "GET",
    workspace,
    signal,
    path: DENOMINATIONS_PATH,
  });
}

export function replaceCashDenominations(
  workspace: PosWorkspaceScope,
  body: ReplaceCashDenominationsRequest,
  signal?: AbortSignal,
): Promise<OrganizationCashDenominationDto[]> {
  return posRequest({
    method: "PUT",
    workspace,
    signal,
    path: DENOMINATIONS_PATH,
    body,
  });
}

export function resolveSetupCompleted(setup: PosOperationalSetupDto | null | undefined): boolean {
  return Boolean(setup?.isComplete || setup?.isCompleted);
}

export function resolveOpeningCashCountMode(
  setup: PosOperationalSetupDto | null | undefined,
): string {
  return setup?.openingCashCountMode?.trim() || setup?.cashCountMode?.trim() || "Required";
}

export function resolveClosingCashCountMode(
  setup: PosOperationalSetupDto | null | undefined,
): string {
  return setup?.closingCashCountMode?.trim() || setup?.cashCountMode?.trim() || "Required";
}

/** Required when mode is Required. Empty/missing defaults to Required (both policies on). */
export function resolveCashCountRequired(cashCountMode: string | null | undefined): boolean {
  if (!cashCountMode || cashCountMode.trim().length === 0) {
    return true;
  }
  return cashCountMode.localeCompare("Required", undefined, { sensitivity: "accent" }) === 0;
}

/** @deprecated Prefer resolveCashCountRequired with opening mode. */
export function resolveOpeningCashRequired(cashCountMode: string | null | undefined): boolean {
  return resolveCashCountRequired(cashCountMode);
}

export function resolveOpeningCashVisible(cashCountMode: string | null | undefined): boolean {
  if (!cashCountMode || cashCountMode.trim().length === 0) {
    return true;
  }
  return cashCountMode.localeCompare("Off", undefined, { sensitivity: "accent" }) !== 0;
}

export function formatDenominationValue(value: number): string {
  if (Number.isInteger(value)) {
    return String(value);
  }
  return value.toFixed(2).replace(/0+$/, "").replace(/\.$/, "");
}

export type CashDenominationCountItem = {
  value: number;
  label?: string | null;
  sortOrder: number;
};

/** Enabled org denominations in cash-handling sort order for shift count helpers. */
export function mapEnabledCashDenominations(
  items: OrganizationCashDenominationDto[] | undefined,
): CashDenominationCountItem[] {
  return [...(items ?? [])]
    .filter((denomination) => denomination.isEnabled)
    .sort((a, b) => a.sortOrder - b.sortOrder || b.value - a.value)
    .map((denomination) => ({
      value: denomination.value,
      label: denomination.displayLabel,
      sortOrder: denomination.sortOrder,
    }));
}
