import { Check, Clock3 } from "lucide-react";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  inventoryTransferStatusPresentation,
} from "@/features/inventory/inventory-transfer-labels";
import { useI18n } from "@/i18n/I18nProvider";

type DiscrepancyCustody = {
  followUpIntent?: string | null;
  status?: string | null;
};

type DiscrepancyFollowUpOptions = {
  /** Family-level remaining to dispatch (Needs fulfillment). */
  remainingToDispatchQty?: number;
  /** Family-level still in transit. */
  openInTransitQty?: number;
};

/** Still open only while return is not yet received at source. */
const OPEN_RETURN_CUSTODY_STATUSES = new Set([
  "AwaitingReturn",
  "ReturnInTransit",
]);

function hasRequestReplacement(custodies: DiscrepancyCustody[]): boolean {
  return custodies.some((c) => c.followUpIntent === "RequestReplacement");
}

function hasPendingReturnCustody(custodies: DiscrepancyCustody[]): boolean {
  return custodies.some(
    (c) => c.status != null && OPEN_RETURN_CUSTODY_STATUSES.has(c.status),
  );
}

function familyFollowUpStillOpen(options?: DiscrepancyFollowUpOptions): boolean {
  const remaining = options?.remainingToDispatchQty ?? 0;
  const inTransit = options?.openInTransitQty ?? 0;
  return remaining > 1e-9 || inTransit > 1e-9;
}

/**
 * Discrepancy chip is "open" (Clock) while either:
 * - return not yet received at source (AwaitingReturn / ReturnInTransit), or
 * - RequestReplacement family fulfillment is still incomplete.
 * Check once returns are received and fulfillment is clear.
 */
export function inventoryTransferHasOpenDiscrepancyFollowUp(
  custodies: DiscrepancyCustody[],
  options?: DiscrepancyFollowUpOptions,
): boolean {
  if (hasPendingReturnCustody(custodies)) {
    return true;
  }
  if (hasRequestReplacement(custodies) && familyFollowUpStillOpen(options)) {
    return true;
  }
  return false;
}

type InventoryTransferStatusChipProps = {
  status: string;
  /** Open discrepancy follow-up (replacement still outstanding). Defaults false. */
  hasOpenDiscrepancyFollowUp?: boolean;
  className?: string;
  "data-testid"?: string;
};

export function InventoryTransferStatusChip({
  status,
  hasOpenDiscrepancyFollowUp = false,
  className,
  "data-testid": testId,
}: InventoryTransferStatusChipProps) {
  const { t } = useI18n();
  const presentation = inventoryTransferStatusPresentation(status, {
    hasOpenDiscrepancyFollowUp,
  });
  const icon =
    presentation.discrepancyIcon === "check" ? (
      <Check className="size-3.5" strokeWidth={2.25} aria-hidden />
    ) : presentation.discrepancyIcon === "clock" ? (
      <Clock3 className="size-3.5" strokeWidth={2.25} aria-hidden />
    ) : undefined;

  return (
    <StatusChip
      tone={presentation.tone}
      icon={icon}
      className={className}
      data-testid={testId}
      data-discrepancy-phase={
        presentation.discrepancyIcon === "check"
          ? "closed"
          : presentation.discrepancyIcon === "clock"
            ? "open"
            : undefined
      }
    >
      {t(presentation.labelKey)}
    </StatusChip>
  );
}
