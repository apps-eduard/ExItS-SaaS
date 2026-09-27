/** Connected Commerce Settings tabs — org-level hub only. */
export const CONNECTED_COMMERCE_TABS = [
  "overview",
  "fulfillment",
  "payments",
  "catalog",
  "orders", // Return Policy (legacy query key kept for deep links)
  "expiry-sale",
  "documents",
] as const;

export type ConnectedCommerceTab = (typeof CONNECTED_COMMERCE_TABS)[number];

export function parseConnectedCommerceTab(value: string | null | undefined): ConnectedCommerceTab {
  if (value === "returns") {
    // Alias for the renamed Return Policy tab.
    return "orders";
  }
  if (value && (CONNECTED_COMMERCE_TABS as readonly string[]).includes(value)) {
    return value as ConnectedCommerceTab;
  }
  return "overview";
}
