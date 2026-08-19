const COMPONENT_STACK_MAX = 512;

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
  if (!trimmed.startsWith("/") || trimmed.includes("?") || trimmed.includes("#")) {
    const pathOnly = trimmed.split("?")[0]?.split("#")[0];
    return pathOnly && pathOnly.startsWith("/") ? pathOnly : undefined;
  }
  return trimmed.length > 0 ? trimmed : undefined;
}

export function currentPathname(): string | undefined {
  if (typeof window === "undefined") {
    return undefined;
  }
  return safePathname(window.location.pathname);
}

export function compactBrowserPlatform(): string | undefined {
  if (typeof navigator === "undefined") {
    return undefined;
  }
  const platform = navigator.platform?.trim();
  const language = navigator.language?.trim();
  const parts = [platform, language].filter((part): part is string => Boolean(part));
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
