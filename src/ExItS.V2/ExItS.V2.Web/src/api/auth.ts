import { clearPlatformAntiforgeryToken, PlatformApiError, platformRequest } from "./platform-http";
import { clearPosAccessToken, setPosAccessToken } from "./pos-access-token";

export const AUTH_ME_PATH = "/api/v1/platform/auth/me";
export const AUTH_LOGIN_PATH = "/api/v1/platform/auth/login";
export const AUTH_LOGOUT_PATH = "/api/v1/platform/auth/logout";
export const AUTH_ORGANIZATIONS_PATH = "/api/v1/platform/auth/organizations";
export const AUTH_ORGANIZATION_CONTEXT_PATH = "/api/v1/platform/auth/organization-context";
export const AUTH_TOKEN_PATH = "/api/v1/platform/auth/token";
export const AUTH_ACCOUNT_PROFILES_PATH = "/api/v1/platform/auth/account-profiles";
export const AUTH_ACCOUNT_PROFILES_SELECT_PATH =
  "/api/v1/platform/auth/account-profiles/select";
export const AUTH_ACCOUNT_PROFILES_ENSURE_PATH =
  "/api/v1/platform/auth/account-profiles/ensure";
export const POS_PRODUCT_CODE = "pinoy-business-pos";
export const SESSION_EXPIRED_ERROR_CODE = "application.auth.session_expired";

export type AccountProfileWire = {
  id: string;
  accountClass: string;
  status: string;
};

export type BrowserSessionSnapshot = {
  sessionId?: string;
  userId?: string;
  username?: string;
  displayName?: string;
  email?: string;
  selectedOrganizationId?: string | null;
  selectedOrganizationDisplayName?: string | null;
  accountClass?: string | null;
  homeOrganizationId?: string | null;
  organizationContextLocked?: boolean;
};

export type EligibleOrganization = {
  organizationId: string;
  displayName: string;
  slug: string;
};

export type PlatformBranch = {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  isPrimary: boolean;
  status: string;
};

function asRecord(value: unknown): Record<string, unknown> {
  return typeof value === "object" && value !== null
    ? (value as Record<string, unknown>)
    : {};
}

function pickString(record: Record<string, unknown>, ...keys: string[]): string | undefined {
  for (const key of keys) {
    const value = record[key];
    if (typeof value === "string" && value.trim()) return value;
  }
  return undefined;
}

export function toBrowserSessionSnapshot(raw: unknown): BrowserSessionSnapshot {
  const r = asRecord(raw);
  return {
    sessionId: pickString(r, "sessionId", "SessionId"),
    userId: pickString(r, "userId", "UserId"),
    username: pickString(r, "username", "Username"),
    displayName: pickString(r, "displayName", "DisplayName"),
    email: pickString(r, "email", "Email"),
    selectedOrganizationId:
      pickString(r, "selectedOrganizationId", "SelectedOrganizationId") ?? null,
    selectedOrganizationDisplayName:
      pickString(r, "selectedOrganizationDisplayName", "SelectedOrganizationDisplayName") ??
      null,
    accountClass: pickString(r, "accountClass", "AccountClass") ?? null,
    homeOrganizationId: pickString(r, "homeOrganizationId", "HomeOrganizationId") ?? null,
    organizationContextLocked:
      r.organizationContextLocked === true || r.OrganizationContextLocked === true,
  };
}

export async function fetchCurrentSession(signal?: AbortSignal): Promise<{
  status: "authenticated" | "unauthenticated" | "expired";
  session: BrowserSessionSnapshot | null;
}> {
  try {
    const body = await platformRequest<unknown>({ path: AUTH_ME_PATH, signal });
    return { status: "authenticated", session: toBrowserSessionSnapshot(body) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      if (error.status === 401 && error.errorCode === SESSION_EXPIRED_ERROR_CODE) {
        return { status: "expired", session: null };
      }
      if (error.status === 401 || error.status === 403) {
        return { status: "unauthenticated", session: null };
      }
    }
    throw error;
  }
}

export async function loginWithPassword(
  usernameOrEmail: string,
  password: string,
): Promise<
  | { ok: true; session: BrowserSessionSnapshot }
  | { ok: false; detail: string; status?: number }
> {
  clearPosAccessToken();
  try {
    const body = await platformRequest<unknown>({
      method: "POST",
      path: AUTH_LOGIN_PATH,
      body: { usernameOrEmail, password },
      skipAntiforgery: true,
    });
    return { ok: true, session: toBrowserSessionSnapshot(body) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return {
        ok: false,
        detail: error.problem.detail ?? error.message,
        status: error.status,
      };
    }
    return { ok: false, detail: "Sign-in failed." };
  }
}

export async function logoutSession(): Promise<void> {
  try {
    await platformRequest<void>({ method: "POST", path: AUTH_LOGOUT_PATH });
  } catch (error) {
    if (!(error instanceof PlatformApiError && error.status === 401)) {
      throw error;
    }
  } finally {
    clearPlatformAntiforgeryToken();
    clearPosAccessToken();
  }
}

function toAccountProfileWire(raw: unknown): AccountProfileWire | null {
  const r = asRecord(raw);
  const id = pickString(r, "id", "Id", "accountProfileId", "AccountProfileId");
  const accountClass = pickString(r, "accountClass", "AccountClass");
  const status = pickString(r, "status", "Status") ?? "";
  if (!id || !accountClass) return null;
  return { id, accountClass, status };
}

export async function listAccountProfiles(
  signal?: AbortSignal,
): Promise<
  | { ok: true; profiles: AccountProfileWire[] }
  | { ok: false; status: number; error: unknown }
> {
  try {
    const body = await platformRequest<unknown>({
      path: AUTH_ACCOUNT_PROFILES_PATH,
      signal,
    });
    const items = Array.isArray(body) ? body : [];
    return {
      ok: true,
      profiles: items
        .map(toAccountProfileWire)
        .filter((item): item is AccountProfileWire => item !== null),
    };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, error };
    }
    throw error;
  }
}

export async function ensureAccountProfile(
  accountClass: "Personal" | "Organization",
  signal?: AbortSignal,
): Promise<
  | { ok: true; profile: AccountProfileWire }
  | { ok: false; status: number; error: unknown }
> {
  try {
    const body = await platformRequest<unknown>({
      method: "POST",
      path: AUTH_ACCOUNT_PROFILES_ENSURE_PATH,
      body: { accountClass },
      signal,
    });
    const profile = toAccountProfileWire(body);
    if (!profile) {
      return {
        ok: false,
        status: 500,
        error: new Error("Account profile ensure returned an empty profile."),
      };
    }
    return { ok: true, profile };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, error };
    }
    throw error;
  }
}

export async function selectAccountProfile(
  accountProfileId: string,
  signal?: AbortSignal,
): Promise<
  | { ok: true; session: BrowserSessionSnapshot }
  | { ok: false; status: number; error: unknown }
> {
  try {
    const body = await platformRequest<unknown>({
      method: "POST",
      path: AUTH_ACCOUNT_PROFILES_SELECT_PATH,
      body: { accountProfileId },
      signal,
    });
    return { ok: true, session: toBrowserSessionSnapshot(body) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, error };
    }
    throw error;
  }
}

export async function listEligibleOrganizations(
  signal?: AbortSignal,
): Promise<EligibleOrganization[]> {
  const body = await platformRequest<unknown>({
    path: AUTH_ORGANIZATIONS_PATH,
    signal,
  });
  if (!Array.isArray(body)) return [];
  return body
    .map((item) => {
      const r = asRecord(item);
      const organizationId = pickString(r, "organizationId", "OrganizationId");
      const displayName = pickString(r, "displayName", "DisplayName") ?? "";
      const slug = pickString(r, "slug", "Slug") ?? "";
      if (!organizationId) return null;
      return { organizationId, displayName, slug };
    })
    .filter((item): item is EligibleOrganization => item !== null);
}

export async function listOrganizationBranches(
  organizationId: string,
  signal?: AbortSignal,
): Promise<PlatformBranch[]> {
  const body = await platformRequest<unknown>({
    path: `/api/v1/platform/organizations/${organizationId}/branches`,
    signal,
  });
  if (!Array.isArray(body)) return [];
  return body
    .map((item) => {
      const r = asRecord(item);
      const id = pickString(r, "id", "Id", "branchId", "BranchId");
      if (!id) return null;
      return {
        id,
        organizationId:
          pickString(r, "organizationId", "OrganizationId") ?? organizationId,
        code: pickString(r, "code", "Code") ?? "",
        name: pickString(r, "name", "Name") ?? "",
        isPrimary: r.isPrimary === true || r.IsPrimary === true,
        status: pickString(r, "status", "Status") ?? "",
      };
    })
    .filter((item): item is PlatformBranch => item !== null);
}

export async function bindWorkspaceWithSessionGrant(
  organizationId: string,
  branchId: string,
  signal?: AbortSignal,
): Promise<{ ok: true } | { ok: false; detail: string; status?: number }> {
  try {
    await platformRequest<void>({
      method: "PUT",
      path: AUTH_ORGANIZATION_CONTEXT_PATH,
      body: { organizationId },
      signal,
    });
    await platformRequest<void>({
      method: "PUT",
      path: `/api/v1/platform/organizations/${organizationId}/branch-context`,
      body: { branchId },
      signal,
    });
    const grantRaw = await platformRequest<Record<string, unknown>>({
      method: "POST",
      path: AUTH_TOKEN_PATH,
      body: {
        grantType: "session",
        organizationId,
        productCode: POS_PRODUCT_CODE,
      },
      signal,
    });
    const accessToken = pickString(grantRaw, "accessToken", "AccessToken");
    const productAccessAllowed =
      grantRaw.productAccessAllowed === true ||
      grantRaw.ProductAccessAllowed === true;
    if (!accessToken || !productAccessAllowed) {
      clearPosAccessToken();
      return {
        ok: false,
        detail: "Product access was denied for this organization.",
        status: 403,
      };
    }
    setPosAccessToken(accessToken);
    return { ok: true };
  } catch (error) {
    clearPosAccessToken();
    if (error instanceof PlatformApiError) {
      return {
        ok: false,
        detail: error.problem.detail ?? error.message,
        status: error.status,
      };
    }
    return { ok: false, detail: "Workspace bind failed." };
  }
}

export function resolveDisplayName(session: BrowserSessionSnapshot | null): string {
  if (!session) return "";
  return (
    session.displayName?.trim() ||
    session.username?.trim() ||
    session.email?.trim() ||
    ""
  );
}

export function resolveInitials(session: BrowserSessionSnapshot | null): string {
  const name = resolveDisplayName(session);
  if (!name) return "?";
  const parts = name.split(/\s+/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0]![0] ?? ""}${parts[1]![0] ?? ""}`.toUpperCase();
  }
  return name.slice(0, 2).toUpperCase();
}
