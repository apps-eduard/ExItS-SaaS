import {
  getRememberedAntiforgeryHeader,
  isAntiforgeryExemptPath,
  isMutationMethod,
  PlatformAntiforgeryDefaults,
  rememberAntiforgeryBootstrap,
  type AntiforgeryBootstrap,
} from "@/api/platform-auth/platform-antiforgery";

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
  selectedOrganizationId?: string | null;
  selectedOrganizationDisplayName?: string | null;
  organizationSelectionState?: string;
  accountClass?: string | null;
  homeOrganizationId?: string | null;
  organizationContextLocked?: boolean;
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
export const AUTH_ORGANIZATIONS_PATH = "/api/v1/platform/auth/organizations";
export const AUTH_ORGANIZATION_CONTEXT_PATH = "/api/v1/platform/auth/organization-context";
export const AUTH_PRODUCT_ACCESS_EFFECTIVE_PATH = "/api/v1/platform/auth/product-access/effective";
export const AUTH_ACCOUNT_PROFILES_PATH = "/api/v1/platform/auth/account-profiles";
export const AUTH_ACCOUNT_PROFILE_SELECT_PATH = "/api/v1/platform/auth/account-profiles/select";
export const PLM_PUBLIC_SURFACE = "pinoy-loan-manager";
export const PLM_PRODUCT_CODE = "pinoy-loan-manager";
export const ACCOUNT_SCOPE_DENIED_ERROR_CODE = "application.auth.account_scope_denied";
export const ORGANIZATION_CONTEXT_REQUIRED_ERROR_CODE =
  "application.auth.organization_context_required";
export const LOCAL_VALIDATION_ENABLED_PATH = "/api/v1/platform/local-validation/enabled";
export const LOCAL_VALIDATION_IDENTITIES_PATH =
  "/api/v1/platform/local-validation/quick-login-identities";

export const SESSION_EXPIRED_ERROR_CODE = "application.auth.session_expired";

export type PlatformProblem = {
  errorCode?: string;
  detail?: string;
};

export type PlatformApiJsonInit = RequestInit & {
  /** When true, skip PWEB-20 antiforgery bootstrap (login/register exempt paths also skip). */
  skipAntiforgery?: boolean;
};

async function bootstrapAntiforgeryToken(signal?: AbortSignal): Promise<void> {
  const response = await fetch(platformApiUrl(PlatformAntiforgeryDefaults.tokenPath), {
    method: "GET",
    credentials: "include",
    headers: { Accept: "application/json" },
    signal,
  });
  if (!response.ok) {
    throw new Error(`Platform antiforgery bootstrap failed (${response.status}).`);
  }
  const text = await response.text();
  const payload = (text ? JSON.parse(text) : null) as AntiforgeryBootstrap | null;
  if (!payload?.token || typeof payload.token !== "string") {
    throw new Error("Platform antiforgery bootstrap returned no token.");
  }
  rememberAntiforgeryBootstrap({
    headerName: payload.headerName || PlatformAntiforgeryDefaults.headerName,
    token: payload.token,
  });
}

async function ensureAntiforgeryToken(signal?: AbortSignal): Promise<void> {
  if (getRememberedAntiforgeryHeader()) {
    return;
  }
  await bootstrapAntiforgeryToken(signal);
}

export async function platformApiJson<T>(
  path: string,
  init?: PlatformApiJsonInit,
): Promise<{ status: number; body: T | null }> {
  const { skipAntiforgery, ...requestInit } = init ?? {};
  const method = (requestInit.method ?? "GET").toUpperCase();
  const headers = new Headers(requestInit.headers);
  if (!headers.has("Accept")) {
    headers.set("Accept", "application/json");
  }

  if (isMutationMethod(method) && !skipAntiforgery && !isAntiforgeryExemptPath(path)) {
    await ensureAntiforgeryToken(
      requestInit.signal instanceof AbortSignal ? requestInit.signal : undefined,
    );
    const antiforgery = getRememberedAntiforgeryHeader();
    if (antiforgery) {
      headers.set(antiforgery.headerName, antiforgery.token);
    }
  }

  const response = await fetch(platformApiUrl(path), {
    ...requestInit,
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
