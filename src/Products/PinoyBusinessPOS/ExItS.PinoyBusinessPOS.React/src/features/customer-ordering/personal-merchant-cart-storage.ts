import type { PersonalMerchantCartLine, PersonalMerchantCartState } from "@/features/customer-ordering/personal-merchant-cart";
import { EMPTY_PERSONAL_MERCHANT_CART } from "@/features/customer-ordering/personal-merchant-cart";

export const PERSONAL_MERCHANT_CART_SCHEMA_VERSION = 1;
export const PERSONAL_MERCHANT_CART_STORAGE_PREFIX = "exits.personal.cart.v1:";
export const PERSONAL_MERCHANT_CART_MAX_LINE_QUANTITY = 999;
export const PERSONAL_MERCHANT_CART_MAX_LINES = 100;

export type PersistedPersonalMerchantCart = {
  version: number;
  sellerOrganizationId: string | null;
  organizationDisplayName: string | null;
  lines: PersonalMerchantCartLine[];
};

export function personalMerchantCartStorageKey(accountKey: string): string {
  return `${PERSONAL_MERCHANT_CART_STORAGE_PREFIX}${accountKey}`;
}

function isFinitePositiveQuantity(value: unknown): value is number {
  return typeof value === "number" && Number.isFinite(value) && value > 0 && value <= PERSONAL_MERCHANT_CART_MAX_LINE_QUANTITY;
}

function isGuidLike(value: unknown): value is string {
  return typeof value === "string" && /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(value.trim());
}

function parseLine(raw: unknown): PersonalMerchantCartLine | null {
  if (!raw || typeof raw !== "object") {
    return null;
  }
  const row = raw as Record<string, unknown>;
  if (!isGuidLike(row.productId) && typeof row.productId !== "string") {
    return null;
  }
  const productId = String(row.productId).trim();
  if (!productId || productId.length > 64) {
    return null;
  }
  const name = typeof row.name === "string" ? row.name.trim() : "";
  if (!name || name.length > 200) {
    return null;
  }
  if (!isFinitePositiveQuantity(row.quantity)) {
    return null;
  }
  const quantity = Math.floor(row.quantity);
  if (quantity < 1) {
    return null;
  }
  const unitPrice =
    typeof row.unitPrice === "number" && Number.isFinite(row.unitPrice) && row.unitPrice >= 0
      ? row.unitPrice
      : null;
  if (unitPrice == null) {
    return null;
  }
  const unitOfMeasure = typeof row.unitOfMeasure === "string" ? row.unitOfMeasure.trim() : "";
  if (!unitOfMeasure || unitOfMeasure.length > 32) {
    return null;
  }
  const sku =
    row.sku == null
      ? null
      : typeof row.sku === "string"
        ? row.sku.trim().slice(0, 64) || null
        : null;

  return {
    productId,
    name,
    sku,
    unitOfMeasure,
    unitPrice,
    quantity,
  };
}

/** Validate and normalize persisted cart; returns empty cart on any malformation. */
export function parsePersistedPersonalMerchantCart(raw: unknown): PersonalMerchantCartState {
  if (!raw || typeof raw !== "object") {
    return EMPTY_PERSONAL_MERCHANT_CART;
  }
  const doc = raw as Record<string, unknown>;
  if (doc.version !== PERSONAL_MERCHANT_CART_SCHEMA_VERSION) {
    return EMPTY_PERSONAL_MERCHANT_CART;
  }

  const sellerOrganizationId =
    doc.sellerOrganizationId == null
      ? null
      : typeof doc.sellerOrganizationId === "string" && isGuidLike(doc.sellerOrganizationId)
        ? doc.sellerOrganizationId.trim()
        : null;

  if (doc.sellerOrganizationId != null && sellerOrganizationId == null) {
    return EMPTY_PERSONAL_MERCHANT_CART;
  }

  const organizationDisplayName =
    doc.organizationDisplayName == null
      ? null
      : typeof doc.organizationDisplayName === "string"
        ? doc.organizationDisplayName.trim().slice(0, 128) || null
        : null;

  if (!Array.isArray(doc.lines)) {
    return EMPTY_PERSONAL_MERCHANT_CART;
  }
  if (doc.lines.length > PERSONAL_MERCHANT_CART_MAX_LINES) {
    return EMPTY_PERSONAL_MERCHANT_CART;
  }

  const lines: PersonalMerchantCartLine[] = [];
  const seen = new Set<string>();
  for (const entry of doc.lines) {
    const line = parseLine(entry);
    if (!line) {
      return EMPTY_PERSONAL_MERCHANT_CART;
    }
    if (seen.has(line.productId)) {
      return EMPTY_PERSONAL_MERCHANT_CART;
    }
    seen.add(line.productId);
    lines.push(line);
  }

  if (lines.length > 0 && !sellerOrganizationId) {
    return EMPTY_PERSONAL_MERCHANT_CART;
  }

  return {
    sellerOrganizationId,
    organizationDisplayName,
    lines,
  };
}

export function loadPersonalMerchantCartFromStorage(accountKey: string | null | undefined): PersonalMerchantCartState {
  if (!accountKey || typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return EMPTY_PERSONAL_MERCHANT_CART;
  }
  try {
    const raw = window.localStorage.getItem(personalMerchantCartStorageKey(accountKey));
    if (!raw) {
      return EMPTY_PERSONAL_MERCHANT_CART;
    }
    return parsePersistedPersonalMerchantCart(JSON.parse(raw) as unknown);
  } catch {
    return EMPTY_PERSONAL_MERCHANT_CART;
  }
}

export function savePersonalMerchantCartToStorage(
  accountKey: string | null | undefined,
  state: PersonalMerchantCartState,
): void {
  if (!accountKey || typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return;
  }
  const key = personalMerchantCartStorageKey(accountKey);
  try {
    if (!state.sellerOrganizationId && state.lines.length === 0) {
      window.localStorage.removeItem(key);
      return;
    }
    const payload: PersistedPersonalMerchantCart = {
      version: PERSONAL_MERCHANT_CART_SCHEMA_VERSION,
      sellerOrganizationId: state.sellerOrganizationId,
      organizationDisplayName: state.organizationDisplayName,
      lines: state.lines,
    };
    window.localStorage.setItem(key, JSON.stringify(payload));
  } catch {
    // Quota / private mode — fail soft; in-memory cart still works for the session.
  }
}

export function clearPersonalMerchantCartStorage(accountKey: string | null | undefined): void {
  if (!accountKey || typeof window === "undefined" || typeof window.localStorage === "undefined") {
    return;
  }
  try {
    window.localStorage.removeItem(personalMerchantCartStorageKey(accountKey));
  } catch {
    // ignore
  }
}
