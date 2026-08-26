import type { PosCashierShiftDto } from "@/api/pos/pos-shifts-client";

/** Historical opening policy: prefer snapshot field, legacy mode as fallback. */
export function resolveHistoricalOpeningMode(
  shift: Pick<PosCashierShiftDto, "effectiveOpeningCashCountMode" | "effectiveCashCountMode">,
): string {
  const opening = shift.effectiveOpeningCashCountMode?.trim();
  if (opening) {
    return opening;
  }
  return shift.effectiveCashCountMode?.trim() || "Optional";
}

/** Historical closing policy: prefer snapshot field, legacy mode as fallback. */
export function resolveHistoricalClosingMode(
  shift: Pick<PosCashierShiftDto, "effectiveClosingCashCountMode" | "effectiveCashCountMode">,
): string {
  const closing = shift.effectiveClosingCashCountMode?.trim();
  if (closing) {
    return closing;
  }
  return shift.effectiveCashCountMode?.trim() || "Optional";
}

/**
 * Closing counted vs skipped.
 * Prefer explicit closingCashCountState; otherwise null amount = not counted.
 */
export function resolveClosingCashCounted(
  shift: Pick<PosCashierShiftDto, "closingCashCountState" | "closingCashAmount">,
): boolean {
  const state = shift.closingCashCountState?.trim();
  if (state) {
    if (state.localeCompare("Counted", undefined, { sensitivity: "accent" }) === 0) {
      return true;
    }
    if (
      state.localeCompare("NotPerformed", undefined, { sensitivity: "accent" }) === 0 ||
      state.localeCompare("NotRequired", undefined, { sensitivity: "accent" }) === 0
    ) {
      return false;
    }
  }
  return shift.closingCashAmount != null;
}

export type CashCountModeKind = "Optional" | "Required" | "Off" | "Unknown";

export function classifyCashCountMode(mode: string | null | undefined): CashCountModeKind {
  const value = mode?.trim() ?? "";
  if (value.localeCompare("Required", undefined, { sensitivity: "accent" }) === 0) {
    return "Required";
  }
  if (value.localeCompare("Optional", undefined, { sensitivity: "accent" }) === 0) {
    return "Optional";
  }
  if (value.localeCompare("Off", undefined, { sensitivity: "accent" }) === 0) {
    return "Off";
  }
  return "Unknown";
}

export type VarianceKind = "balanced" | "over" | "short";

export function classifyCashVariance(variance: number): VarianceKind {
  if (variance === 0) {
    return "balanced";
  }
  return variance > 0 ? "over" : "short";
}
