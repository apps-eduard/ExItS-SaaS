const MAILPIT_UI_PORT = 8025;

export function currentBrowserHostname(): string | undefined {
  if (typeof window === "undefined") {
    return undefined;
  }
  const hostname = window.location.hostname?.trim();
  return hostname && hostname.length > 0 ? hostname : undefined;
}

export function resolveMailpitConvenienceUrl(hostname = currentBrowserHostname()): string | null {
  const resolved = hostname?.trim();
  if (!resolved) {
    return null;
  }
  return `http://${resolved}:${MAILPIT_UI_PORT}`;
}
