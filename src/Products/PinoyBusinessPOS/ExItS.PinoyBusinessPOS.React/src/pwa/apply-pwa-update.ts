export type PwaUpdateApplyGuard = () => boolean;

let cartLineCountGetter: (() => number) | null = null;

export function registerCartLineCountGetter(getter: (() => number) | null): void {
  cartLineCountGetter = getter;
}

/** Block PWA refresh while the session cart still has lines. */
export function canApplyPwaUpdate(): boolean {
  if (cartLineCountGetter && cartLineCountGetter() > 0) {
    return false;
  }
  return true;
}

export function applyPwaUpdateIfAllowed(
  apply: () => void,
  guard: PwaUpdateApplyGuard = canApplyPwaUpdate,
): boolean {
  if (!guard()) {
    return false;
  }
  apply();
  return true;
}
