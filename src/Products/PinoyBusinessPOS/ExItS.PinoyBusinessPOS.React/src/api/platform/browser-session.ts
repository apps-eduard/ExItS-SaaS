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

export function assertBrowserStorageHasNoBearerToken(storage: Storage): void {
  for (let index = 0; index < storage.length; index += 1) {
    const key = storage.key(index);
    if (!key) {
      continue;
    }
    const value = storage.getItem(key) ?? "";
    if (/\bBearer\b/i.test(value) || /accessToken/i.test(key) || /accessToken/i.test(value)) {
      throw new Error("Bearer or access tokens must not be persisted in browser storage.");
    }
  }
}

export const AUTH_LOGIN_PATH = "/api/v1/platform/auth/login";
export const AUTH_ME_PATH = "/api/v1/platform/auth/me";
export const AUTH_LOGOUT_PATH = "/api/v1/platform/auth/logout";
export const AUTH_REGISTER_PATH = "/api/v1/platform/auth/register";
export const AUTH_FORGOT_PASSWORD_PATH = "/api/v1/platform/auth/forgot-password";
export const AUTH_ACTIVATE_PATH = "/api/v1/platform/auth/activate-account";
export const AUTH_RESET_PASSWORD_PATH = "/api/v1/platform/auth/reset-password";
export const AUTH_ACCOUNT_PROFILES_PATH = "/api/v1/platform/auth/account-profiles";
export const AUTH_ACCOUNT_PROFILES_SELECT_PATH = "/api/v1/platform/auth/account-profiles/select";
export const AUTH_ACCOUNT_PROFILES_ENSURE_PATH = "/api/v1/platform/auth/account-profiles/ensure";
export const AUTH_ORGANIZATIONS_PATH = "/api/v1/platform/auth/organizations";
export const AUTH_ORGANIZATION_CONTEXT_PATH = "/api/v1/platform/auth/organization-context";
export const AUTH_TOKEN_PATH = "/api/v1/platform/auth/token";
export const LOCAL_VALIDATION_ENABLED_PATH = "/api/v1/platform/local-validation/enabled";
export const LOCAL_VALIDATION_IDENTITIES_PATH =
  "/api/v1/platform/local-validation/quick-login-identities";

export const POS_PRODUCT_CODE = "pinoy-business-pos";
/** Server allow-listed public surface for Personal/POS activation and password-reset emails. */
export const POS_PUBLIC_SURFACE = "pinoy-business-pos";

export const SESSION_EXPIRED_ERROR_CODE = "application.auth.session_expired";

export type PlatformProblem = {
  errorCode?: string;
  detail?: string;
  title?: string;
  status?: number;
};

export function organizationBranchesPath(organizationId: string): string {
  return `/api/v1/platform/organizations/${organizationId}/branches`;
}

export function organizationBranchContextPath(organizationId: string): string {
  return `/api/v1/platform/organizations/${organizationId}/branch-context`;
}
