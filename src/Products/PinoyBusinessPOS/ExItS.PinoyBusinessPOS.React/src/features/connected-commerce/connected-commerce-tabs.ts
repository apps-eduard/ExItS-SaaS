/** Connected Commerce Settings tabs — org-level hub only. */
export const CONNECTED_COMMERCE_TABS = [
  "overview",
  "fulfillment",
  "payments",
  "catalog",
  "orders",
  "documents",
] as const;

export type ConnectedCommerceTab = (typeof CONNECTED_COMMERCE_TABS)[number];

export function parseConnectedCommerceTab(value: string | null | undefined): ConnectedCommerceTab {
  if (value && (CONNECTED_COMMERCE_TABS as readonly string[]).includes(value)) {
    return value as ConnectedCommerceTab;
  }
  return "overview";
}
