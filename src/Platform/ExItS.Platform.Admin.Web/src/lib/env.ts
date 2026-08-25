function readRuntimeApiBaseUrl(): string {
  if (typeof window === "undefined") {
    return "";
  }
  const runtime = window.__EXITS_PLATFORM_ADMIN_WEB__?.platformApiBaseUrl;
  return typeof runtime === "string" ? runtime.trim() : "";
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

  const runtime = window.__EXITS_PLATFORM_ADMIN_WEB__;
  if (runtime?.platformApiSameOrigin === true) {
    return true;
  }

  return readRuntimeApiBaseUrl().toLowerCase() === "same-origin";
}

/**
 * Local Validation Vite DEV: always same-origin /api.
 * Vite proxies /api → 127.0.0.1:8091 for both localhost and Tailscale page hosts.
 * Do NOT route browser calls to http://<tailscale>:8091 (firewall / remote clients).
 */
export function resolvePlatformApiBaseUrl(): string {
  // DEV Local Validation: prefer Vite same-origin proxy regardless of page hostname.
  if (import.meta.env.DEV && import.meta.env.VITE_LOCAL_VALIDATION_TOOLS === "true") {
    return "";
  }

  if (isPlatformApiSameOrigin()) {
    return "";
  }

  const runtime = readRuntimeApiBaseUrl();
  if (runtime.length > 0 && runtime.toLowerCase() !== "same-origin") {
    return runtime.replace(/\/+$/, "");
  }

  if (import.meta.env.DEV) {
    // Generic Vite DEV without LV tools flag: still prefer same-origin when no explicit base.
    return "";
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
