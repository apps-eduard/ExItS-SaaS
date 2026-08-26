const COMPONENT_STACK_MAX = 512;

const SENSITIVE_QUERY_KEYS = [
  "token",
  "code",
  "ticket",
  "access_token",
  "refresh_token",
  "password",
  "newpassword",
  "oldpassword",
  "confirmpassword",
  "state",
];

export function isAbortError(error: unknown): boolean {
  if (error instanceof DOMException && error.name === "AbortError") {
    return true;
  }
  return error instanceof Error && error.name === "AbortError";
}

export function safePathname(pathname: string | undefined): string | undefined {
  if (typeof pathname !== "string") {
    return undefined;
  }
  const trimmed = pathname.trim();
  if (!trimmed.startsWith("/")) {
    return undefined;
  }
  const pathOnly = trimmed.split("?")[0]?.split("#")[0];
  return pathOnly && pathOnly.startsWith("/") ? pathOnly : undefined;
}

export function sanitizeApiPath(path: string | undefined): string | undefined {
  if (typeof path !== "string") {
    return undefined;
  }

  const trimmed = path.trim();
  if (trimmed.length === 0) {
    return undefined;
  }

  const withoutOrigin = trimmed.replace(/^https?:\/\/[^/]+/i, "");
  const pathOnly = withoutOrigin.split("?")[0]?.split("#")[0] ?? withoutOrigin;
  if (!pathOnly.startsWith("/")) {
    return pathOnly.startsWith("api/") ? `/${pathOnly}` : undefined;
  }

  return pathOnly;
}

export function stripSensitiveQueryFromUrl(value: string | undefined): string | undefined {
  if (typeof value !== "string" || value.trim().length === 0) {
    return undefined;
  }

  try {
    const url = value.startsWith("http") ? new URL(value) : new URL(value, "http://localhost");
    for (const key of [...url.searchParams.keys()]) {
      if (SENSITIVE_QUERY_KEYS.includes(key.toLowerCase())) {
        url.searchParams.delete(key);
      }
    }
    if (value.startsWith("http")) {
      return `${url.pathname}${url.search}${url.hash}`.replace(/^\/?/, "/");
    }
    return `${url.pathname}${url.search}${url.hash}`;
  } catch {
    return safePathname(value.split("?")[0]);
  }
}

export function currentPathname(): string | undefined {
  if (typeof window === "undefined") {
    return undefined;
  }
  return safePathname(window.location.pathname);
}

export function readNetworkOnline(): boolean | undefined {
  if (typeof navigator === "undefined" || typeof navigator.onLine !== "boolean") {
    return undefined;
  }
  return navigator.onLine;
}

export function readBrowserInfo(): { name?: string; version?: string; platform?: string } {
  if (typeof navigator === "undefined") {
    return {};
  }

  const ua = navigator.userAgent;
  const platform = navigator.platform?.trim();
  const matchers: Array<{ name: string; pattern: RegExp }> = [
    { name: "Edge", pattern: /Edg\/([\d.]+)/ },
    { name: "Chrome", pattern: /Chrome\/([\d.]+)/ },
    { name: "Firefox", pattern: /Firefox\/([\d.]+)/ },
    { name: "Safari", pattern: /Version\/([\d.]+).*Safari/ },
  ];

  for (const matcher of matchers) {
    const match = ua.match(matcher.pattern);
    if (match) {
      return { name: matcher.name, version: match[1], platform };
    }
  }

  return platform ? { platform } : {};
}

export function compactBrowserPlatform(): string | undefined {
  const info = readBrowserInfo();
  const parts = [info.platform, info.name, info.version].filter(Boolean);
  return parts.length > 0 ? parts.join("; ") : undefined;
}

export function capComponentStack(value: string | undefined): string | undefined {
  if (typeof value !== "string") {
    return undefined;
  }
  const trimmed = value.trim();
  if (trimmed.length === 0) {
    return undefined;
  }
  if (trimmed.length <= COMPONENT_STACK_MAX) {
    return trimmed;
  }
  return `${trimmed.slice(0, COMPONENT_STACK_MAX)}…`;
}

export function createErrorReference(): string {
  const bytes = new Uint8Array(3);
  crypto.getRandomValues(bytes);
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, "0"))
    .join("")
    .toUpperCase();
  return `ERR-${hex}`;
}

export function presentText(value: string | undefined): string | undefined {
  if (typeof value !== "string") {
    return undefined;
  }
  const trimmed = value.trim();
  return trimmed.length > 0 ? trimmed : undefined;
}

export function containsSensitiveValue(text: string, secret: string): boolean {
  return secret.length > 0 && text.includes(secret);
}
