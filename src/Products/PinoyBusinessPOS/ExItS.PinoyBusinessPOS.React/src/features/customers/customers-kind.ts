export type KindFilter = "all" | "people" | "businesses";

export function parseKindForTest(raw: string | null): KindFilter {
  if (raw === "people" || raw === "businesses" || raw === "all") return raw;
  return "all";
}
