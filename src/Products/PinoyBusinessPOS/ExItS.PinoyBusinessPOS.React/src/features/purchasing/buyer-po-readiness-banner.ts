/**
 * Buyer-safe commerce readiness messaging.
 * Maps blocker categories from ConnectedSupplierCommerceReadiness — never internal codes/details.
 */

import type { MessageKey } from "@/i18n/messages";

export const BUYER_PO_BLOCKER_CATEGORIES = [
  "Fulfillment",
  "NoUsableMethod",
  "Payment",
  "Catalog",
  "Contact",
  "Credit",
] as const;

export type BuyerPoBlockerCategory = (typeof BUYER_PO_BLOCKER_CATEGORIES)[number];

const CATEGORY_SET = new Set<string>(BUYER_PO_BLOCKER_CATEGORIES);

export function normalizeBuyerPoBlockerCategories(
  raw: ReadonlyArray<string> | null | undefined,
): BuyerPoBlockerCategory[] {
  if (!raw || raw.length === 0) return [];
  const present = new Set<string>();
  for (const item of raw) {
    const trimmed = item.trim();
    if (CATEGORY_SET.has(trimmed)) present.add(trimmed);
  }
  return BUYER_PO_BLOCKER_CATEGORIES.filter((c) => present.has(c));
}

const SINGLE_BODY_KEYS: Record<BuyerPoBlockerCategory, MessageKey> = {
  Fulfillment: "purchasing.supplierNotReady.reason.fulfillment",
  NoUsableMethod: "purchasing.supplierNotReady.reason.noUsableMethod",
  Payment: "purchasing.supplierNotReady.reason.payment",
  Catalog: "purchasing.supplierNotReady.reason.catalog",
  Contact: "purchasing.supplierNotReady.reason.contact",
  Credit: "purchasing.supplierNotReady.reason.credit",
};

const ISSUE_LABEL_KEYS: Record<BuyerPoBlockerCategory, MessageKey> = {
  Fulfillment: "purchasing.supplierNotReady.issue.fulfillment",
  NoUsableMethod: "purchasing.supplierNotReady.issue.noUsableMethod",
  Payment: "purchasing.supplierNotReady.issue.payment",
  Catalog: "purchasing.supplierNotReady.issue.catalog",
  Contact: "purchasing.supplierNotReady.issue.contact",
  Credit: "purchasing.supplierNotReady.issue.credit",
};

const PHRASE_KEYS: Record<BuyerPoBlockerCategory, MessageKey> = {
  Fulfillment: "purchasing.supplierNotReady.phrase.fulfillment",
  NoUsableMethod: "purchasing.supplierNotReady.phrase.noUsableMethod",
  Payment: "purchasing.supplierNotReady.phrase.payment",
  Catalog: "purchasing.supplierNotReady.phrase.catalog",
  Contact: "purchasing.supplierNotReady.phrase.contact",
  Credit: "purchasing.supplierNotReady.phrase.credit",
};

export type BuyerPoNotReadyCopy = {
  bodyKey: MessageKey;
  /** For multi-blocker body; null for single / fallback. */
  listPhrase: string | null;
  issueLine: string | null;
  categories: BuyerPoBlockerCategory[];
};

function joinPhrases(parts: string[]): string {
  if (parts.length === 0) return "";
  if (parts.length === 1) return parts[0]!;
  if (parts.length === 2) return `${parts[0]} and ${parts[1]}`;
  return `${parts.slice(0, -1).join(", ")}, and ${parts[parts.length - 1]}`;
}

/**
 * Builds buyer-facing copy from blocker categories only.
 * Never accepts requirement titles/details.
 */
export function buildBuyerPoNotReadyCopy(
  blockerCategories: ReadonlyArray<string> | null | undefined,
  t: (key: MessageKey) => string,
): BuyerPoNotReadyCopy {
  const categories = normalizeBuyerPoBlockerCategories(blockerCategories);
  if (categories.length === 0) {
    return {
      bodyKey: "purchasing.supplierNotReadyBody",
      listPhrase: null,
      issueLine: null,
      categories,
    };
  }

  if (categories.length === 1) {
    const only = categories[0]!;
    return {
      bodyKey: SINGLE_BODY_KEYS[only],
      listPhrase: null,
      issueLine: `${t("purchasing.supplierNotReady.issuePrefix")} ${t(ISSUE_LABEL_KEYS[only])}`,
      categories,
    };
  }

  const phrases = categories.map((c) => t(PHRASE_KEYS[c]));
  const listPhrase = joinPhrases(phrases);
  const issueLabels = categories.map((c) => t(ISSUE_LABEL_KEYS[c]));
  return {
    bodyKey: "purchasing.supplierNotReady.reason.multiple",
    listPhrase,
    issueLine: `${t("purchasing.supplierNotReady.issuesPrefix")} ${issueLabels.join(" · ")}`,
    categories,
  };
}
