import type { InventoryTransferDamagedCustodyDecisionCode } from "@/api/pos/pos-inventory-transfer-client";

export const OTHER_REASONS_FORCE_RETURN = ["WrongItem", "WrongVariant", "Expired"] as const;

export type OtherReasonForceReturnCode = (typeof OTHER_REASONS_FORCE_RETURN)[number];

export function requiresActualProduct(reasonCode: string): boolean {
  return reasonCode === "WrongItem" || reasonCode === "WrongVariant";
}

/** Wrong item/variant source receive restores sellable stock; no inspection UI. */
export function restoresDirectlyToSellableOnSourceReceive(reasonCode: string): boolean {
  return requiresActualProduct(reasonCode);
}

/** Wrong item/variant cannot claim the expected transfer product as the actual SKU. */
export function isActualProductSameAsExpected(
  reasonCode: string,
  expectedProductId: string | null | undefined,
  actualProductId: string | null | undefined,
): boolean {
  if (!requiresActualProduct(reasonCode)) {
    return false;
  }
  const expected = expectedProductId?.trim();
  const actual = actualProductId?.trim();
  if (!expected || !actual) {
    return false;
  }
  return expected.toLowerCase() === actual.toLowerCase();
}

export function forcesReturnToSource(reasonCode: string): boolean {
  return (OTHER_REASONS_FORCE_RETURN as readonly string[]).includes(reasonCode);
}

export function resolveDefaultOtherCustody(
  reasonCode: string,
): InventoryTransferDamagedCustodyDecisionCode {
  if (forcesReturnToSource(reasonCode)) {
    return "ReturnToSource";
  }
  return "KeepAtDestination";
}
