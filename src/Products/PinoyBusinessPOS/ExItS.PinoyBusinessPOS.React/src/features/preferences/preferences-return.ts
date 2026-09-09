export type PreferencesLocationState = {
  returnTo?: string;
};

const PREFERENCES_RETURN_STORAGE_KEY = "exits.preferences.returnTo";

/** True when a nav destination opens the preferences drawer. */
export function isPreferencesDestination(to: string): boolean {
  return to === "/settings/preferences" || to.startsWith("/settings/preferences?");
}

/** Safe in-app return path after closing the preferences drawer. */
export function isSafePreferencesReturnPath(path: string | null | undefined): path is string {
  if (!path || !path.startsWith("/") || path.startsWith("//")) {
    return false;
  }
  if (path === "/settings/preferences" || path.startsWith("/settings/preferences?")) {
    return false;
  }
  return true;
}

/** Location state for opening preferences from the current content route. */
export function preferencesNavigationState(
  currentPathname: string,
  currentSearch = "",
): PreferencesLocationState | undefined {
  if (
    currentPathname === "/settings/preferences" ||
    currentPathname.startsWith("/settings/preferences/")
  ) {
    return undefined;
  }
  const returnTo = `${currentPathname}${currentSearch}`;
  return isSafePreferencesReturnPath(returnTo) ? { returnTo } : undefined;
}

/** Persist return path when opening preferences from a nav control. */
export function capturePreferencesReturnFrom(
  currentPathname: string,
  currentSearch = "",
): void {
  const state = preferencesNavigationState(currentPathname, currentSearch);
  if (state?.returnTo) {
    rememberPreferencesReturnTo(state.returnTo);
  }
}

function readStoredReturnTo(): string | null {
  try {
    const stored = sessionStorage.getItem(PREFERENCES_RETURN_STORAGE_KEY);
    return isSafePreferencesReturnPath(stored) ? stored : null;
  } catch {
    return null;
  }
}

export function rememberPreferencesReturnTo(path: string): void {
  if (!isSafePreferencesReturnPath(path)) {
    return;
  }
  try {
    sessionStorage.setItem(PREFERENCES_RETURN_STORAGE_KEY, path);
  } catch {
    // Ignore quota / private-mode failures; location state still works.
  }
}

export function clearPreferencesReturnTo(): void {
  try {
    sessionStorage.removeItem(PREFERENCES_RETURN_STORAGE_KEY);
  } catch {
    // Ignore storage failures.
  }
}

function returnToFromLocationState(locationState: unknown): string | null {
  if (
    !locationState ||
    typeof locationState !== "object" ||
    !("returnTo" in locationState) ||
    typeof (locationState as PreferencesLocationState).returnTo !== "string"
  ) {
    return null;
  }
  const returnTo = (locationState as PreferencesLocationState).returnTo;
  return isSafePreferencesReturnPath(returnTo) ? returnTo : null;
}

/** Resolve where to go after closing preferences (does not clear storage). */
export function resolvePreferencesReturnTo(
  locationState: unknown,
  fallback: string,
): string {
  return returnToFromLocationState(locationState) ?? readStoredReturnTo() ?? fallback;
}

/** Resolve and clear stored return path (call when leaving preferences). */
export function takePreferencesReturnTo(locationState: unknown, fallback: string): string {
  const resolved = resolvePreferencesReturnTo(locationState, fallback);
  clearPreferencesReturnTo();
  return resolved;
}
