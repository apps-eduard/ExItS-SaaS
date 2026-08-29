import { WASTE_LOSS_REASONS, type WasteLossReasonCode } from "@/api/pos/pos-waste-loss-client";

export type ExpiredWasteQuickFlowParams = {
  productId: string;
  lotId: string;
  /** UI convenience only — form must revalidate against current lot qty. */
  quantity?: number;
  source?: "expiration";
};

export function isWasteLossReasonCode(value: string | null | undefined): value is WasteLossReasonCode {
  return Boolean(value && (WASTE_LOSS_REASONS as readonly string[]).includes(value));
}

/** Build Waste/Loss create URL for an exact expired lot from Expiration. */
export function buildExpiredWasteQuickFlowHref(params: ExpiredWasteQuickFlowParams): string {
  const query = new URLSearchParams({
    productId: params.productId,
    lotId: params.lotId,
    reason: "Expired",
    source: params.source ?? "expiration",
  });
  if (
    params.quantity != null &&
    Number.isFinite(params.quantity) &&
    params.quantity > 0
  ) {
    query.set("quantity", String(params.quantity));
  }
  return `/inventory/waste-loss/new?${query.toString()}`;
}

export function parseWasteLossPrefillQuantity(raw: string | null): number | null {
  if (raw == null || raw.trim() === "") {
    return null;
  }
  const qty = Number(raw);
  if (!Number.isFinite(qty) || qty <= 0) {
    return null;
  }
  return qty;
}
