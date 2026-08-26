const MAILPIT_UI_PORT = 8025;

/** Prefer IPv4 loopback when the SPA is on localhost (avoids ::1 refused). */
export function resolveMailpitConvenienceUrl(
  hostname = typeof window !== "undefined" ? window.location.hostname : undefined,
): string | null {
  const resolved = hostname?.trim();
  if (!resolved) {
    return null;
  }
  const host = resolved === "localhost" ? "127.0.0.1" : resolved;
  return `http://${host}:${MAILPIT_UI_PORT}/`;
}
