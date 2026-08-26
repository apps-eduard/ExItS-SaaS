const SECRET_KEY_PATTERN =
  /(access[_-]?token|refresh[_-]?token|authorization|bearer|password|antiforgery|csrf|invitation[_-]?token|registration[_-]?token|recovery|secret|api[_-]?key|xsrf|cookie|set-cookie|x-xsrf-token)/i;

const SECRET_VALUE_PATTERN =
  /\b(Bearer\s+[A-Za-z0-9\-._~+/]+=*|eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+|[0-9a-f]{32,}|[A-Za-z0-9_-]{24,}=*)\b/gi;

const COOKIE_HEADER_PATTERN = /\b(Cookie|Set-Cookie|X-XSRF-TOKEN)\s*[:=]\s*[^\s;]+/gi;

/** Origin + pathname only — never search/hash (invitation tokens etc.). */
export function safeDiagnosticLocation(
  hrefOrUrl?: string | null,
  pathnameFallback?: string | null,
): { url: string; pathname: string } {
  try {
    if (hrefOrUrl) {
      const parsed = new URL(
        hrefOrUrl,
        typeof window !== "undefined" ? window.location.origin : "https://local.invalid",
      );
      return {
        url: `${parsed.origin}${parsed.pathname}`,
        pathname: parsed.pathname || pathnameFallback || "/",
      };
    }
  } catch {
    // fall through
  }

  if (typeof window !== "undefined") {
    return {
      url: `${window.location.origin}${window.location.pathname}`,
      pathname: window.location.pathname || pathnameFallback || "/",
    };
  }

  return {
    url: pathnameFallback ? pathnameFallback : "(unknown)",
    pathname: pathnameFallback || "(unknown)",
  };
}

export function redactDiagnosticText(value: string | null | undefined): string {
  if (!value) {
    return "";
  }
  return value
    .replace(COOKIE_HEADER_PATTERN, "[REDACTED]")
    .replace(/\bpassword\s*=\s*\S+/gi, "password=[REDACTED]")
    .replace(SECRET_VALUE_PATTERN, "[REDACTED]");
}

function isPlainObject(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/**
 * Normalize unknown thrown values for copyable AI reports.
 * Never JSON.stringify arbitrary objects (may contain tokens / customer / Personal data).
 */
export function safeDiagnosticError(error: unknown): {
  name: string;
  message: string;
  stack: string | null;
} {
  if (error instanceof Error) {
    return {
      name: error.name || "Error",
      message: redactDiagnosticText(error.message || "(no message)"),
      stack: error.stack ? redactDiagnosticText(error.stack) : null,
    };
  }

  if (typeof error === "string") {
    return {
      name: "Error",
      message: redactDiagnosticText(error),
      stack: null,
    };
  }

  if (isPlainObject(error)) {
    const keys = Object.keys(error);
    const secretKeys = keys.filter((key) => SECRET_KEY_PATTERN.test(key));
    if (secretKeys.length > 0) {
      return {
        name: "Error",
        message: `Non-Error object with sensitive keys omitted (${secretKeys.length} key(s))`,
        stack: null,
      };
    }
    const name = typeof error.name === "string" ? error.name : "Error";
    const message =
      typeof error.message === "string"
        ? redactDiagnosticText(error.message)
        : "Non-Error object (details omitted for privacy)";
    return { name, message, stack: null };
  }

  return {
    name: "Error",
    message: "Unknown non-Error value (details omitted for privacy)",
    stack: null,
  };
}
