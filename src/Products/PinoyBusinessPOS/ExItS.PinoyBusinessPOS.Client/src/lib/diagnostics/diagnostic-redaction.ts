const SENSITIVE_PATTERN =
  /(password|passwd|pin|token|bearer|authorization|cookie|session|secret|otp|cvv|payload|stack)/i;

export function isAbortError(error: unknown): boolean {
  if (error instanceof DOMException && error.name === "AbortError") {
    return true;
  }
  return error instanceof Error && error.name === "AbortError";
}

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

export function redactIfSensitive(value: string | undefined): string | undefined {
  const text = presentText(value);
  if (!text) {
    return undefined;
  }
  if (SENSITIVE_PATTERN.test(text)) {
    return "[redacted]";
  }
  return text;
}

export function assertNoForbiddenDiagnostics(report: string, sentinels: readonly string[]): void {
  for (const sentinel of sentinels) {
    if (report.includes(sentinel)) {
      throw new Error(`Diagnostic report leaked sentinel: ${sentinel}`);
    }
  }
}
