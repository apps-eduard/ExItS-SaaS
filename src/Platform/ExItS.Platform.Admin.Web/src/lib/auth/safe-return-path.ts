const DEFAULT_AUTHENTICATED_PATH = "/admin";

export function sanitizeReturnPath(raw: string | null | undefined): string | null {
  if (raw == null) {
    return null;
  }

  let value = raw.trim();
  if (value.length === 0) {
    return null;
  }

  try {
    value = decodeURIComponent(value);
  } catch {
    return null;
  }

  value = value.trim();
  if (!value.startsWith("/") || value.startsWith("//") || value.includes("\\")) {
    return null;
  }

  if (/^[a-zA-Z][a-zA-Z0-9+.-]*:/.test(value)) {
    return null;
  }

  let parsed: URL;
  try {
    parsed = new URL(value, "https://exits.invalid");
  } catch {
    return null;
  }

  if (parsed.origin !== "https://exits.invalid") {
    return null;
  }

  const path = `${parsed.pathname}${parsed.search}${parsed.hash}`;
  if (path.startsWith("/admin/login")) {
    return null;
  }

  return path;
}

export function resolvePostLoginPath(rawReturn: string | null | undefined): string {
  return sanitizeReturnPath(rawReturn) ?? DEFAULT_AUTHENTICATED_PATH;
}

export function buildLoginPath(options?: {
  returnPath?: string | null;
  notice?: "session-expired";
}): string {
  const params = new URLSearchParams();
  const safeReturn = sanitizeReturnPath(options?.returnPath ?? null);
  if (safeReturn) {
    params.set("return", safeReturn);
  }
  if (options?.notice === "session-expired") {
    params.set("notice", "session-expired");
  }
  const query = params.toString();
  return query.length > 0 ? `/admin/login?${query}` : "/admin/login";
}

export { DEFAULT_AUTHENTICATED_PATH };
