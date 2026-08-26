const SAFE_ERROR_CODE = /^[a-zA-Z][a-zA-Z0-9_-]{0,32}(\.[a-zA-Z0-9_-]{1,32}){1,5}$/;
const SAFE_CORRELATION_ID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

export function safePathname(pathname: string | undefined): string {
  if (typeof pathname !== "string" || pathname.trim().length === 0) {
    return "/";
  }
  const pathOnly = pathname.trim().split("?")[0]?.split("#")[0] ?? "/";
  return pathOnly.startsWith("/") ? pathOnly : "/";
}

export function currentPathname(): string {
  if (typeof window === "undefined") {
    return "/";
  }
  return safePathname(window.location.pathname);
}

export function compactBrowserPlatform(): string {
  if (typeof navigator === "undefined") {
    return "unknown";
  }
  const platform = navigator.platform?.trim() || "unknown";
  const language = navigator.language?.trim();
  return language ? `${platform}; ${language}` : platform;
}

export function createErrorReference(): string {
  const bytes = new Uint8Array(2);
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

export function allowlistedErrorCode(value: string | undefined): string | undefined {
  const text = presentText(value);
  if (!text || !SAFE_ERROR_CODE.test(text)) {
    return undefined;
  }
  return text;
}

export function allowlistedCorrelationId(value: string | undefined): string | undefined {
  const text = presentText(value);
  if (!text || !SAFE_CORRELATION_ID.test(text)) {
    return undefined;
  }
  return text;
}

export function allowlistedHttpStatus(value: number | undefined): number | undefined {
  if (typeof value !== "number" || !Number.isInteger(value) || value < 100 || value > 599) {
    return undefined;
  }
  return value;
}

export function assertNoForbiddenDiagnostics(report: string, sentinels: readonly string[]): void {
  for (const sentinel of sentinels) {
    if (report.includes(sentinel)) {
      throw new Error(`Diagnostic report leaked sentinel: ${sentinel}`);
    }
  }
}
