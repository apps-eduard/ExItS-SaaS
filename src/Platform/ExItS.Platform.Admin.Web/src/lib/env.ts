function readRuntimeApiBaseUrl(): string {
  if (typeof window === "undefined") {
    return "";
  }
  const runtime = window.__EXITS_PLATFORM_ADMIN_WEB__?.platformApiBaseUrl;
  return typeof runtime === "string" ? runtime.trim() : "";
}

function isLoopbackHostname(hostname: string): boolean {
  const host = hostname.trim().toLowerCase();
  return host === "localhost" || host === "127.0.0.1" || host === "[::1]" || host === "::1";
}

/**
 * Vite DEV on Tailscale/LAN: call Platform API on the same host at :8091.
 * Loopback keeps empty base URL so Vite proxies /api → 127.0.0.1:8091.
 */
export function resolveDevLanPlatformApiBaseUrl(
  hostname: string,
  protocol: string,
  apiPort = 8091,
): string {
  const host = hostname.trim();
  if (!host || isLoopbackHostname(host)) {
    return "";
  }
  const scheme = protocol === "https:" ? "https:" : "http:";
  return `${scheme}//${host}:${apiPort}`;
}

export function isLocalValidationToolsEnabled(): boolean {
  if (typeof window !== "undefined" && window.__EXITS_PLATFORM_ADMIN_WEB__?.localValidationToolsEnabled === true) {
    return true;
  }

  // Vite DEV / Local Validation: allow weak passwords (1 char) to match Platform API Start env.
  return import.meta.env.VITE_LOCAL_VALIDATION_TOOLS === "true";
}

export function isPlatformApiSameOrigin(): boolean {
  if (typeof window === "undefined") {
    return false;
  }

  // Tailscale/LAN pages must not force same-origin (Vite /api proxy is unreliable from phones).
  if (!isLoopbackHostname(window.location.hostname)) {
    return false;
  }

  const runtime = window.__EXITS_PLATFORM_ADMIN_WEB__;
  if (runtime?.platformApiSameOrigin === true) {
    return true;
  }

  return readRuntimeApiBaseUrl().toLowerCase() === "same-origin";
}

export function resolvePlatformApiBaseUrl(): string {
  if (isPlatformApiSameOrigin()) {
    return "";
  }

  const runtime = readRuntimeApiBaseUrl();
  if (runtime.length > 0 && runtime.toLowerCase() !== "same-origin") {
    return runtime.replace(/\/+$/, "");
  }

  if (import.meta.env.DEV && typeof window !== "undefined") {
    return resolveDevLanPlatformApiBaseUrl(window.location.hostname, window.location.protocol);
  }

  const compiled = import.meta.env.VITE_PLATFORM_API_BASE_URL ?? "";
  return compiled.trim().replace(/\/+$/, "");
}

export function displayPlatformApiBaseUrl(): string {
  const resolved = resolvePlatformApiBaseUrl();
  return resolved.length > 0 ? resolved : "(same-origin)";
}

export function resolveFrontendBuildSha(): string {
  const compiled = import.meta.env.VITE_BUILD_SHA?.trim();
  if (compiled) {
    return compiled;
  }
  if (typeof window === "undefined") {
    return "unknown";
  }
  const runtime = window.__EXITS_PLATFORM_ADMIN_WEB__?.buildSha;
  return typeof runtime === "string" && runtime.trim().length > 0 ? runtime.trim() : "unknown";
}

export function getFrontendRuntimeStatus() {
  return {
    app: "Platform Admin React",
    frontendMode: import.meta.env.MODE,
    buildSha: resolveFrontendBuildSha(),
    apiBaseUrl: displayPlatformApiBaseUrl(),
    localValidationToolsEnabled: isLocalValidationToolsEnabled(),
  };
}

export const env = {
  get platformApiBaseUrl(): string {
    return resolvePlatformApiBaseUrl();
  },
  get localValidationToolsEnabled(): boolean {
    return isLocalValidationToolsEnabled();
  },
};
