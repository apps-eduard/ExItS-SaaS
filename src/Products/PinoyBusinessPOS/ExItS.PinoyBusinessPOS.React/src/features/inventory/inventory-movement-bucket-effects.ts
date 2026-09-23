/** Semantic inventory bucket deltas for movement history (mirrors StockMovementPresentation). */

export type MovementBucketEffects = {
  physicalDelta: number;
  sellableDelta: number;
  damagedDelta: number;
  inspectionHoldDelta: number;
};

/**
 * Damaged transfer hold must never present as +sellable.
 * Write-off / recovery reclassify buckets without implying generic stock ±qty.
 */
export function describeMovementBucketEffects(
  movementType: string,
  signedQuantityEffect: number,
): MovementBucketEffects {
  const abs = Math.abs(signedQuantityEffect);
  switch (movementType) {
    case "TransferOut":
      return { physicalDelta: -abs, sellableDelta: -abs, damagedDelta: 0, inspectionHoldDelta: 0 };
    case "TransferIn":
    case "TransferCancelRestore":
      return { physicalDelta: abs, sellableDelta: abs, damagedDelta: 0, inspectionHoldDelta: 0 };
    case "TransferDamageHold":
      return { physicalDelta: abs, sellableDelta: 0, damagedDelta: abs, inspectionHoldDelta: 0 };
    case "TransferDamageRecovery":
      return { physicalDelta: 0, sellableDelta: abs, damagedDelta: 0, inspectionHoldDelta: -abs };
    case "TransferDamageReturnOut":
      return { physicalDelta: -abs, sellableDelta: 0, damagedDelta: -abs, inspectionHoldDelta: 0 };
    case "TransferDamageReturnIn":
      return { physicalDelta: abs, sellableDelta: 0, damagedDelta: 0, inspectionHoldDelta: abs };
    case "TransferDamageWriteOff":
      return { physicalDelta: 0, sellableDelta: 0, damagedDelta: abs, inspectionHoldDelta: -abs };
    default:
      return {
        physicalDelta: signedQuantityEffect,
        sellableDelta: signedQuantityEffect,
        damagedDelta: 0,
        inspectionHoldDelta: 0,
      };
  }
}

export function formatSignedBucketQty(value: number): string {
  if (value === 0) {
    return "0";
  }
  return value > 0 ? `+${value}` : `${value}`;
}

/** True when the type needs explicit Physical/Sellable/Damaged lines (not raw qty alone). */
export function movementNeedsBucketBreakdown(movementType: string): boolean {
  return (
    movementType === "TransferDamageHold" ||
    movementType === "TransferDamageRecovery" ||
    movementType === "TransferDamageReturnOut" ||
    movementType === "TransferDamageReturnIn" ||
    movementType === "TransferDamageWriteOff" ||
    movementType === "TransferIn" ||
    movementType === "TransferOut"
  );
}
