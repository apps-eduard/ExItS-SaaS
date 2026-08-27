export function captureEmailCallbackToken(search: string): string | null {
  const token = new URLSearchParams(search).get("token")?.trim() ?? "";
  return token.length > 0 ? token : null;
}

/** Remove `?token=` from the address bar after capture. Memory-only thereafter. */
export function scrubTokenFromBrowserLocation(pathname: string): void {
  if (typeof window === "undefined") {
    return;
  }
  const next = `${pathname}${window.location.hash}`;
  window.history.replaceState(window.history.state, "", next);
}

export function assertStorageHasNoAuthToken(storage: Storage): void {
  for (let index = 0; index < storage.length; index += 1) {
    const key = storage.key(index);
    if (!key) {
      continue;
    }
    const value = storage.getItem(key) ?? "";
    if (/token=/i.test(key) || /token=/i.test(value)) {
      throw new Error("Email callback tokens must not be persisted.");
    }
  }
}
