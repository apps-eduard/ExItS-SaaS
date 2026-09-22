/**
 * Seller Shared Catalog bulk-selection helpers.
 * Prefer authoritative API flags; derive from effective share + resolved prices when omitted
 * so Shared rows never appear as Needs price with disabled Stop sharing.
 */

export type ShareRowActionability = {
  supplierProductId: string;
  canShare?: boolean;
  canStopSharing?: boolean;
  isInventoryTracked?: boolean;
  isEligible?: boolean;
  isEffectivelyShared?: boolean;
  hasValidPoPrice?: boolean;
  resolvedPoPrice?: number | null;
  sellingPrice?: number | null;
  defaultPoPrice?: number | null;
  sharingStatus?: string;
};

export type ShareRowHintReason = "needsTracking" | "needsPrice";

/** Default PO when > 0, else Selling when > 0 — mirrors server ResolvePoPrice. */
export function resolvePoPrice(item: ShareRowActionability): number | null {
  if (item.resolvedPoPrice != null && item.resolvedPoPrice > 0) {
    return item.resolvedPoPrice;
  }
  if (item.defaultPoPrice != null && item.defaultPoPrice > 0) {
    return item.defaultPoPrice;
  }
  if (item.sellingPrice != null && item.sellingPrice > 0) {
    return item.sellingPrice;
  }
  return null;
}

export function rowHasValidPoPrice(item: ShareRowActionability): boolean {
  if (item.hasValidPoPrice === true) {
    return true;
  }
  return resolvePoPrice(item) != null;
}

export function rowCanStopSharing(item: ShareRowActionability): boolean {
  if (item.canStopSharing === true) {
    return true;
  }
  // Shared rows must remain stoppable even when canStopSharing was omitted/defaulted.
  return item.isEffectivelyShared === true || item.sharingStatus === "Shared";
}

export function rowCanShare(item: ShareRowActionability): boolean {
  if (item.canShare === true) {
    return true;
  }
  if (rowCanStopSharing(item)) {
    return false;
  }
  return (
    item.isEligible === true
    && item.isEffectivelyShared !== true
    && item.sharingStatus !== "Shared"
    && rowHasValidPoPrice(item)
  );
}

export function isShareRowSelectable(item: ShareRowActionability): boolean {
  return rowCanShare(item) || rowCanStopSharing(item);
}

export function actionableShareProductIds(
  items: readonly ShareRowActionability[],
): string[] {
  return items.filter(isShareRowSelectable).map((item) => item.supplierProductId);
}

export function shareRowHintReason(item: ShareRowActionability): ShareRowHintReason | null {
  if (isShareRowSelectable(item)) {
    return null;
  }

  if (item.isInventoryTracked === false || item.sharingStatus === "Ineligible") {
    return "needsTracking";
  }

  if (!rowHasValidPoPrice(item) && item.isEligible === true) {
    return "needsPrice";
  }

  return null;
}

/** Needs price only when eligible, not stoppable-shared, and no usable resolved PO price. */
export function shareRowNeedsPrice(item: ShareRowActionability): boolean {
  if (rowCanStopSharing(item) || rowHasValidPoPrice(item)) {
    return false;
  }
  return item.isEligible === true;
}
