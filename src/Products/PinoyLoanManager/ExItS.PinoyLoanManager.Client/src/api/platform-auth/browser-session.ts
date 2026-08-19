export const PLATFORM_API_BASE_PATH = "/platform-api";

const ABSOLUTE_API_PATTERN = /^https?:\/\//i;

export function platformApiUrl(path: string): string {
  if (ABSOLUTE_API_PATTERN.test(path) || path.includes("://") || /:(?:8091)\b/.test(path)) {
    throw new Error("Platform API calls must stay on the relative /platform-api origin.");
  }

  const normalized = path.startsWith("/") ? path : `/${path}`;
  return `${PLATFORM_API_BASE_PATH}${normalized}`;
}

export type PlatformLoginWire = {
  sessionId?: string;
  userId?: string;
  username?: string;
  displayName?: string;
  email?: string;
  expiresAtUtc?: string;
  absoluteExpiresAtUtc?: string;
  sessionToken?: string;
};

export type BrowserSessionSnapshot = Omit<PlatformLoginWire, "sessionToken">;

export function toBrowserSessionSnapshot(wire: PlatformLoginWire): BrowserSessionSnapshot {
  const safe: BrowserSessionSnapshot & { sessionToken?: string } = { ...wire };
  delete safe.sessionToken;
  return safe;
}

export function assertBrowserStorageHasNoSessionToken(storage: Storage): void {
  for (let index = 0; index < storage.length; index += 1) {
    const key = storage.key(index);
    if (!key) {
      continue;
    }
    const value = storage.getItem(key) ?? "";
    if (/sessionToken/i.test(key) || /sessionToken/i.test(value)) {
      throw new Error("SessionToken must not be persisted in browser storage.");
    }
  }
}

export const AUTH_LOGIN_PATH = "/api/v1/platform/auth/login";
export const AUTH_ME_PATH = "/api/v1/platform/auth/me";
export const AUTH_LOGOUT_PATH = "/api/v1/platform/auth/logout";
export const AUTH_REGISTER_PATH = "/api/v1/platform/auth/register";
export const AUTH_ACTIVATE_PATH = "/api/v1/platform/auth/activate-account";
export const AUTH_FORGOT_PASSWORD_PATH = "/api/v1/platform/auth/forgot-password";
export const AUTH_RESET_PASSWORD_PATH = "/api/v1/platform/auth/reset-password";
export const PLM_PUBLIC_SURFACE = "pinoy-loan-manager";
export const LOCAL_VALIDATION_ENABLED_PATH = "/api/v1/platform/local-validation/enabled";
export const LOCAL_VALIDATION_IDENTITIES_PATH =
  "/api/v1/platform/local-validation/quick-login-identities";

export const SESSION_EXPIRED_ERROR_CODE = "application.auth.session_expired";

export type PlatformProblem = {
  errorCode?: string;
  detail?: string;
};

export async function platformApiJson<T>(
  path: string,
  init?: RequestInit,
): Promise<{ status: number; body: T | null }> {
  const headers = new Headers(init?.headers);
  if (!headers.has("Accept")) {
    headers.set("Accept", "application/json");
  }
  const response = await fetch(platformApiUrl(path), {
    ...init,
    credentials: "include",
    headers,
  });
  if (response.status === 204) {
    return { status: response.status, body: null };
  }
  const text = await response.text();
  if (!text) {
    return { status: response.status, body: null };
  }
  return { status: response.status, body: JSON.parse(text) as T };
}
