function readRuntimeApiBaseUrl(): string {
  if (typeof window === "undefined") {
    return "";
  }
  const runtime = window.__EXITS_PLATFORM_ADMIN_WEB__?.platformApiBaseUrl;
  return typeof runtime === "string" ? runtime.trim() : "";
}

export function isLocalValidationToolsEnabled(): boolean {
  if (typeof window === "undefined") {
    return false;
  }

  return window.__EXITS_PLATFORM_ADMIN_WEB__?.localValidationToolsEnabled === true;
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

export function resolvePlatformApiBaseUrl(): string {
  if (isPlatformApiSameOrigin()) {
    return "";
  }
  const runtime = readRuntimeApiBaseUrl();
  if (runtime.length > 0) {
    return runtime.replace(/\/+$/, "");
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
