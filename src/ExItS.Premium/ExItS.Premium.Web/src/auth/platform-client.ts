const PREFIX = "/platform-api";

export class PlatformApiError extends Error {
  readonly status: number;
  readonly code?: string;

  constructor(message: string, status: number, code?: string) {
    super(message);
    this.name = "PlatformApiError";
    this.status = status;
    this.code = code;
  }
}

export type PlatformSession = {
  userId: string;
  displayName: string;
  email: string;
  accountClass: string | null;
  selectedOrganizationId: string | null;
  selectedOrganizationDisplayName: string | null;
  organizationSelectionState: string | null;
  activeOrganizationCount: number;
  homeOrganizationId: string | null;
  organizationContextLocked: boolean;
};

export type AccountProfile = {
  id: string;
  accountClass: string;
  status: string;
};

export type EligibleOrganization = {
  organizationId: string;
  displayName: string;
  membershipRole: string;
};

type ProblemBody = { detail?: string; title?: string; errorCode?: string };

let csrfToken: string | null = null;

export function clearCachedSecurityToken() {
  csrfToken = null;
}

async function problem(response: Response): Promise<never> {
  const body = (await response.json().catch(() => null)) as ProblemBody | null;
  throw new PlatformApiError(body?.detail || body?.title || "The request could not be completed.", response.status, body?.errorCode);
}

async function csrfHeaders(): Promise<Record<string, string>> {
  if (!csrfToken) {
    const response = await fetch(`${PREFIX}/api/v1/platform/antiforgery/token`, { credentials: "include" });
    if (!response.ok) await problem(response);
    const body = (await response.json()) as { token?: string };
    if (!body.token) throw new PlatformApiError("The account service did not provide a security token.", response.status);
    csrfToken = body.token;
  }
  return { "X-XSRF-TOKEN": csrfToken };
}

async function send(path: string, method: string, body?: unknown) {
  const headers: Record<string, string> = { accept: "application/json", ...(await csrfHeaders()) };
  if (body !== undefined) headers["content-type"] = "application/json";
  const response = await fetch(`${PREFIX}${path}`, {
    method,
    credentials: "include",
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  if (response.status === 401 || response.status === 403) csrfToken = null;
  return response;
}

function sessionFrom(body: {
  userId?: string;
  displayName?: string;
  email?: string;
  accountClass?: string | null;
  selectedOrganizationId?: string | null;
  selectedOrganizationDisplayName?: string | null;
  organizationSelectionState?: string | null;
  activeOrganizationCount?: number;
  homeOrganizationId?: string | null;
  organizationContextLocked?: boolean;
}): PlatformSession {
  return {
    userId: body.userId ?? "",
    displayName: body.displayName?.trim() || "ExItS",
    email: body.email ?? "",
    accountClass: body.accountClass ?? null,
    selectedOrganizationId: body.selectedOrganizationId ?? null,
    selectedOrganizationDisplayName: body.selectedOrganizationDisplayName ?? null,
    organizationSelectionState: body.organizationSelectionState ?? null,
    activeOrganizationCount: body.activeOrganizationCount ?? 0,
    homeOrganizationId: body.homeOrganizationId ?? null,
    organizationContextLocked: body.organizationContextLocked === true,
  };
}

/** Cookie session only. The login body token is discarded and never stored. */
export async function loginWithPassword(email: string, password: string) {
  const response = await send("/api/v1/platform/auth/login", "POST", { usernameOrEmail: email, password });
  if (!response.ok) await problem(response);
  await response.json().catch(() => null);
}

export async function currentSession(): Promise<PlatformSession | null> {
  const response = await fetch(`${PREFIX}/api/v1/platform/auth/me`, { credentials: "include", headers: { accept: "application/json" } });
  if (response.status === 401 || response.status === 403) return null;
  if (!response.ok) await problem(response);
  return sessionFrom((await response.json()) as Parameters<typeof sessionFrom>[0]);
}

export async function listAccountProfiles(): Promise<AccountProfile[]> {
  const rows = await readJson<Array<{ id?: string; accountClass?: string; status?: string }>>("/api/v1/platform/auth/account-profiles");
  return rows.flatMap((row) => {
    if (!row.id || !row.accountClass) return [];
    return [{ id: row.id, accountClass: row.accountClass, status: row.status ?? "" }];
  });
}

/** Rebinds the current cookie session. The returned token is discarded. */
export async function selectAccountProfile(accountProfileId: string) {
  const response = await send("/api/v1/platform/auth/account-profiles/select", "POST", { accountProfileId });
  if (!response.ok) await problem(response);
  await response.json().catch(() => null);
}

export async function listEligibleOrganizations(): Promise<EligibleOrganization[]> {
  const rows = await readJson<Array<{ organizationId?: string; displayName?: string; membershipRole?: string }>>("/api/v1/platform/auth/organizations");
  return rows.flatMap((row) => {
    if (!row.organizationId || !row.displayName) return [];
    return [{ organizationId: row.organizationId, displayName: row.displayName, membershipRole: row.membershipRole ?? "" }];
  });
}

export async function selectOrganization(organizationId: string) {
  const response = await send("/api/v1/platform/auth/organization-context", "PUT", { organizationId });
  if (!response.ok) await problem(response);
  await response.json().catch(() => null);
}

export async function logoutSession() {
  const response = await send("/api/v1/platform/auth/logout", "POST");
  csrfToken = null;
  if (!response.ok && response.status !== 204 && response.status !== 401) await problem(response);
}

export async function registerPersonalAccount(name: string, email: string) {
  const response = await send("/api/v1/platform/auth/register", "POST", { displayName: name, email });
  if (!response.ok) await problem(response);
  const body = (await response.json()) as { message?: string };
  return body.message || "The account service accepted the registration request.";
}

export async function requestPasswordReset(email: string) {
  const response = await send("/api/v1/platform/auth/forgot-password", "POST", { usernameOrEmail: email });
  if (!response.ok) await problem(response);
  const body = (await response.json()) as { message?: string };
  return body.message || "The account service accepted the reset request.";
}

export async function resetPassword(token: string, newPassword: string) {
  const response = await send("/api/v1/platform/auth/reset-password", "POST", { token, newPassword });
  if (!response.ok) await problem(response);
  const body = (await response.json().catch(() => null)) as { message?: string } | null;
  return body?.message || "The account service accepted the new password.";
}

export function resetTokenFromLocation(search: string) {
  return new URLSearchParams(search).get("token")?.trim() || "";
}

export async function readJson<T>(path: string): Promise<T> {
  const response = await fetch(`${PREFIX}${path}`, { credentials: "include", headers: { accept: "application/json" } });
  if (response.status === 401) throw new PlatformApiError("Your session has ended.", 401);
  if (!response.ok) await problem(response);
  return (await response.json()) as T;
}
