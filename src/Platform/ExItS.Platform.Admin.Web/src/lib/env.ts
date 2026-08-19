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

export function resolvePlatformApiBaseUrl(): string {
  const runtime = readRuntimeApiBaseUrl();
  if (runtime.length > 0) {
    return runtime.replace(/\/+$/, "");
  }
  const compiled = import.meta.env.VITE_PLATFORM_API_BASE_URL ?? "";
  return compiled.trim().replace(/\/+$/, "");
}

export const env = {
  get platformApiBaseUrl(): string {
    return resolvePlatformApiBaseUrl();
  },
  get localValidationToolsEnabled(): boolean {
    return isLocalValidationToolsEnabled();
  },
};
