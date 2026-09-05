/** Pure helpers for stock request UX. */

export function pickPreferredSourceId(
  routes: ReadonlyArray<{ sourceLocationId: string; isPreferred: boolean; isActive: boolean }>,
): string | null {
  const active = routes.filter((r) => r.isActive);
  if (active.length === 0) return null;
  return active.find((r) => r.isPreferred)?.sourceLocationId ?? active[0]?.sourceLocationId ?? null;
}

export function remainingRequestQty(requested: number, fulfilled: number, inProgress: number): number {
  return Math.max(0, requested - fulfilled - inProgress);
}

export function hasConfiguredInternalSource(
  routes: ReadonlyArray<{ isActive: boolean }>,
): boolean {
  return routes.some((r) => r.isActive);
}
