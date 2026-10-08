export type KindFilter = "all" | "people" | "businesses";

/** Organization customer list tabs. Checkout keeps the smaller kind filter. */
export type CustomerListTab = KindFilter | "deactivated";

export function parseKindForTest(raw: string | null): KindFilter {
  if (raw === "people" || raw === "businesses" || raw === "all") return raw;
  return "all";
}

export function parseCustomerListTab(raw: string | null): CustomerListTab {
  if (raw === "deactivated") return "deactivated";
  return parseKindForTest(raw);
}
