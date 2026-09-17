import { roundMoneyAmount } from "@/lib/money-input";
import type { B2bObligationListFilter } from "@/features/b2b-obligations/b2b-obligations-model";

export type SupplierPayableListFilter = B2bObligationListFilter;

export {
  filterB2bObligations as filterSupplierPayables,
  countB2bObligationsByFilter as countSupplierPayablesByFilter,
} from "@/features/b2b-obligations/b2b-obligations-model";

/**
 * Buyer Supplier Credit exposure math (aligned with seller Business Customer policy):
 * Used = Outstanding + Reserved by active POs
 * Available = Approved limit − Outstanding − Reserved
 * Utilization % = Used / Limit × 100
 */
export type SupplierCreditExposureInput = {
  approvedCreditLimit: number | null | undefined;
  outstanding: number;
  reservedByActivePos: number;
};

export type SupplierCreditExposure = {
  usedCredit: number;
  /** Null when utilization/available cannot be computed (no approved limit). */
  availableCredit: number | null;
  /** Raw utilization percent (may exceed 100). Null when no approved limit. */
  utilizationPercent: number | null;
  /** Progress-bar width percent, clamped 0–100. Null when utilization unavailable. */
  progressPercent: number | null;
  isOverLimit: boolean;
  hasApprovedLimit: boolean;
};

export function computeSupplierCreditExposure(
  input: SupplierCreditExposureInput,
): SupplierCreditExposure {
  const outstanding = roundMoneyAmount(Math.max(0, input.outstanding));
  const reserved = roundMoneyAmount(Math.max(0, input.reservedByActivePos));
  const usedCredit = roundMoneyAmount(outstanding + reserved);
  const limit =
    input.approvedCreditLimit != null && Number.isFinite(input.approvedCreditLimit)
      ? roundMoneyAmount(input.approvedCreditLimit)
      : null;
  const hasApprovedLimit = limit != null && limit > 0;

  if (!hasApprovedLimit || limit == null) {
    return {
      usedCredit,
      availableCredit: null,
      utilizationPercent: null,
      progressPercent: null,
      isOverLimit: false,
      hasApprovedLimit: false,
    };
  }

  const availableCredit = roundMoneyAmount(limit - usedCredit);
  const utilizationPercent = roundMoneyAmount((usedCredit / limit) * 100);
  const progressPercent = Math.min(100, Math.max(0, utilizationPercent));
  return {
    usedCredit,
    availableCredit,
    utilizationPercent,
    progressPercent,
    isOverLimit: usedCredit > limit + 1e-9,
    hasApprovedLimit: true,
  };
}

export function formatUtilizationPercent(percent: number): string {
  const rounded = Math.round(percent * 10) / 10;
  if (Number.isInteger(rounded)) {
    return String(rounded);
  }
  return rounded.toFixed(1);
}
