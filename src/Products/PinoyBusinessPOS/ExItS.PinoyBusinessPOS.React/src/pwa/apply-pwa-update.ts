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

/** Apply now, or as soon as the cart guard allows it. No prompt. */
export function applyPwaUpdateWhenAllowed(
  apply: () => void,
  guard: PwaUpdateApplyGuard = canApplyPwaUpdate,
  waitMs = 1000,
  schedule: (callback: () => void, delayMs: number) => number = (callback, delayMs) =>
    window.setTimeout(callback, delayMs),
): () => void {
  let stopped = false;
  const tick = () => {
    if (stopped) {
      return;
    }
    if (applyPwaUpdateIfAllowed(apply, guard)) {
      return;
    }
    schedule(tick, waitMs);
  };
  tick();
  return () => {
    stopped = true;
  };
}
