/**
 * Global Smart Back — shared return-context for POS detail/edit drill-downs.
 * Prefer explicit in-app returnTo; never trust external/open-redirect values.
 */

export type SmartBackLocationState = {
  returnTo?: string;
  /** Marks that returnTo was captured by navigateWithReturn / AppLinkWithReturn. */
  smartBack?: true;
};

const STORAGE_PREFIX = "exits.smartBack.returnTo:";

/** Auth / workspace / binding flows must never become return destinations. */
const BLOCKED_RETURN_PREFIXES = [
  "/login",
  "/auth",
  "/select-workspace",
  "/select-organization",
  "/bind",
  "/device",
  "/activate",
  "/invite",
  "/recover",
  "/pin-recovery",
  "/personal/login",
  "/personal/activate",
  "/personal/recover",
] as const;

export type SmartBackFallbackKey =
  | "sell"
  | "directPurchases"
  | "purchaseOrders"
  | "incomingOrders"
  | "catalog"
  | "suppliers"
  | "customers"
  | "transfers"
  | "quotations"
  | "branches"
  | "expenses"
  | "returns"
  | "inventory"
  | "stockRequests"
  | "stockCounts"
  | "wasteLoss"
  | "stockUse"
  | "production"
  | "areas"
  | "needsAttentionHome";

/** Canonical parent fallbacks for direct/bookmarked entry. */
export const smartBackFallbacks: Record<SmartBackFallbackKey, string> = {
  sell: "/sell",
  directPurchases: "/purchasing/direct-purchases",
  purchaseOrders: "/purchasing/orders",
  incomingOrders: "/purchasing/incoming-orders",
  catalog: "/catalog",
  suppliers: "/suppliers",
  customers: "/customers",
  transfers: "/inventory/transfers",
  quotations: "/quotations",
  branches: "/org/branches",
  expenses: "/expenses",
  returns: "/returns",
  inventory: "/inventory",
  stockRequests: "/inventory/stock-requests",
  stockCounts: "/inventory/stock-counts",
  wasteLoss: "/inventory/waste-loss",
  stockUse: "/inventory/stock-use",
  production: "/inventory/production",
  areas: "/org/areas",
  needsAttentionHome: "/role/manager",
};

export function buildReturnTo(
  pathname: string,
  search = "",
  hash = "",
): string {
  const path = pathname.startsWith("/") ? pathname : `/${pathname}`;
  const q = search && !search.startsWith("?") && search.length > 0 ? `?${search}` : search;
  const h = hash && !hash.startsWith("#") && hash.length > 0 ? `#${hash}` : hash;
  return `${path}${q}${h}`;
}

function normalizeReturnCandidate(raw: string): string {
  const trimmed = raw.trim();
  if (!trimmed) {
    return "";
  }
  try {
    if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(trimmed)) {
      // Absolute URL — only same-origin path is acceptable.
      const url = new URL(trimmed, window.location.origin);
      if (url.origin !== window.location.origin) {
        return "";
      }
      return `${url.pathname}${url.search}${url.hash}`;
    }
  } catch {
    return "";
  }
  return trimmed;
}

/**
 * Accept only internal POS routes. Reject external, protocol-relative, and auth/workspace flows.
 */
export function isSafeAppReturnPath(
  path: string | null | undefined,
  options?: { currentLocation?: string },
): path is string {
  if (!path) {
    return false;
  }
  const normalized = normalizeReturnCandidate(path);
  if (!normalized.startsWith("/") || normalized.startsWith("//")) {
    return false;
  }
  if (normalized.includes("://") || normalized.toLowerCase().includes("javascript:")) {
    return false;
  }
  const pathOnly = normalized.split(/[?#]/)[0] ?? normalized;
  for (const prefix of BLOCKED_RETURN_PREFIXES) {
    if (pathOnly === prefix || pathOnly.startsWith(`${prefix}/`)) {
      return false;
    }
  }
  if (options?.currentLocation) {
    const current = normalizeReturnCandidate(options.currentLocation);
    if (current && normalized === current) {
      return false;
    }
  }
  return true;
}

export function smartBackNavigationState(
  returnTo: string | null | undefined,
): SmartBackLocationState | undefined {
  if (!isSafeAppReturnPath(returnTo)) {
    return undefined;
  }
  return { returnTo, smartBack: true };
}

function storageKeyForDestination(destinationPath: string): string {
  const pathOnly = destinationPath.split(/[?#]/)[0] || destinationPath;
  return `${STORAGE_PREFIX}${pathOnly}`;
}

export function rememberSmartBackReturnTo(
  destinationPath: string,
  returnTo: string,
): void {
  if (!isSafeAppReturnPath(returnTo)) {
    return;
  }
  try {
    sessionStorage.setItem(storageKeyForDestination(destinationPath), returnTo);
  } catch {
    // Ignore quota / private mode; location state still works.
  }
}

export function clearSmartBackReturnTo(destinationPath: string): void {
  try {
    sessionStorage.removeItem(storageKeyForDestination(destinationPath));
  } catch {
    // Ignore.
  }
}

function readStoredReturnTo(destinationPath: string): string | null {
  try {
    const stored = sessionStorage.getItem(storageKeyForDestination(destinationPath));
    return isSafeAppReturnPath(stored, { currentLocation: destinationPath }) ? stored : null;
  } catch {
    return null;
  }
}

function returnToFromLocationState(
  locationState: unknown,
  currentLocation: string,
): string | null {
  if (!locationState || typeof locationState !== "object" || !("returnTo" in locationState)) {
    return null;
  }
  const value = (locationState as SmartBackLocationState).returnTo;
  return typeof value === "string" &&
    isSafeAppReturnPath(value, { currentLocation: currentLocation })
    ? value
    : null;
}

function returnToFromSearchParams(
  searchParams: URLSearchParams | null | undefined,
  currentLocation: string,
): string | null {
  if (!searchParams) {
    return null;
  }
  const raw = searchParams.get("returnTo");
  return isSafeAppReturnPath(raw, { currentLocation }) ? normalizeReturnCandidate(raw!) : null;
}

/**
 * Resolve Back destination:
 * 1) explicit safe returnTo (location state)
 * 2) query ?returnTo=
 * 3) sessionStorage captured for this detail path (refresh)
 * 4) canonical fallback
 */
export function resolveSmartBackTo(args: {
  locationState: unknown;
  currentPathname: string;
  currentSearch?: string;
  currentHash?: string;
  searchParams?: URLSearchParams | null;
  fallback: string;
}): string {
  const current = buildReturnTo(
    args.currentPathname,
    args.currentSearch ?? "",
    args.currentHash ?? "",
  );
  const fromState = returnToFromLocationState(args.locationState, current);
  if (fromState) {
    return fromState;
  }
  const fromQuery = returnToFromSearchParams(args.searchParams, current);
  if (fromQuery) {
    return fromQuery;
  }
  const fromStorage = readStoredReturnTo(args.currentPathname);
  if (fromStorage) {
    return fromStorage;
  }
  const fallback = args.fallback.startsWith("/") ? args.fallback : `/${args.fallback}`;
  return isSafeAppReturnPath(fallback) ? fallback : "/";
}

/** Resolve and clear stored return for this detail page (call when leaving via Back). */
export function takeSmartBackTo(args: {
  locationState: unknown;
  currentPathname: string;
  currentSearch?: string;
  currentHash?: string;
  searchParams?: URLSearchParams | null;
  fallback: string;
}): string {
  const resolved = resolveSmartBackTo(args);
  clearSmartBackReturnTo(args.currentPathname);
  return resolved;
}

/**
 * Build Link `state` that preserves the exact origin (path + search + hash).
 */
export function linkStateWithReturn(from: {
  pathname: string;
  search?: string;
  hash?: string;
}): SmartBackLocationState | undefined {
  const returnTo = buildReturnTo(from.pathname, from.search ?? "", from.hash ?? "");
  return smartBackNavigationState(returnTo);
}

export type NavigateLike = (
  to: string,
  options?: { replace?: boolean; state?: unknown },
) => void;

/**
 * Navigate into a detail/edit page while capturing a safe returnTo for Back.
 */
export function navigateWithReturn(
  navigate: NavigateLike,
  to: string,
  from: { pathname: string; search?: string; hash?: string },
  options?: { replace?: boolean },
): void {
  const state = linkStateWithReturn(from);
  if (state?.returnTo) {
    rememberSmartBackReturnTo(to, state.returnTo);
  }
  navigate(to, { replace: options?.replace, state });
}

/**
 * Whether history.back() is a reasonable option (same-tab in-app push).
 * Prefer explicit returnTo; only use history when no return context exists and
 * the document referrer is same-origin and safe.
 */
export function canUseSafeHistoryBack(args: {
  hasExplicitReturnTo: boolean;
  historyLength: number;
  referrer: string;
  currentLocation: string;
}): boolean {
  if (args.hasExplicitReturnTo) {
    return false;
  }
  if (args.historyLength <= 1) {
    return false;
  }
  if (!args.referrer) {
    return false;
  }
  try {
    const ref = new URL(args.referrer);
    if (ref.origin !== window.location.origin) {
      return false;
    }
    const candidate = `${ref.pathname}${ref.search}${ref.hash}`;
    return isSafeAppReturnPath(candidate, { currentLocation: args.currentLocation });
  } catch {
    return false;
  }
}
