import {
  clearPlatformAntiforgeryToken,
  platformRequest,
  PlatformApiError,
} from "@/api/platform/platform-http";
import {
  AUTH_ACCOUNT_PROFILES_ENSURE_PATH,
  AUTH_ACCOUNT_PROFILES_PATH,
  AUTH_ACCOUNT_PROFILES_SELECT_PATH,
  AUTH_LOGIN_PATH,
  AUTH_LOGOUT_PATH,
  AUTH_ME_PATH,
  AUTH_ORGANIZATION_CONTEXT_PATH,
  AUTH_ORGANIZATIONS_PATH,
  AUTH_TOKEN_PATH,
  AUTH_REGISTER_PATH,
  AUTH_FORGOT_PASSWORD_PATH,
  AUTH_ACTIVATE_PATH,
  AUTH_RESET_PASSWORD_PATH,
  LOCAL_VALIDATION_ENABLED_PATH,
  LOCAL_VALIDATION_IDENTITIES_PATH,
  organizationBranchContextPath,
  organizationBranchesPath,
  POS_PRODUCT_CODE,
  POS_PUBLIC_SURFACE,
  SESSION_EXPIRED_ERROR_CODE,
  toBrowserSessionSnapshot,
  type BrowserSessionSnapshot,
  type PlatformLoginWire,
  type PlatformProblem,
} from "@/api/platform/browser-session";
import { isFrontendLocalValidationMode } from "@/api/platform/local-validation-gate";
import {
  buildAuthLoginFailure,
  type AuthLoginFailureDiagnostic,
} from "@/diagnostics/auth-login-failure";
import { clearPosAccessToken, setPosAccessToken } from "@/api/platform/pos-access-token";
import { clearPosSessionGrant, setPosSessionGrant } from "@/api/platform/pos-session-grant";

export type LoginWithPasswordResult =
  | { ok: true; session: BrowserSessionSnapshot }
  | { ok: false; failure: AuthLoginFailureDiagnostic };

export type QuickLoginIdentity = {
  key?: string;
  username?: string;
  displayName?: string;
  email?: string;
  listLabel?: string;
  organizationName?: string | null;
  organizationRole?: string | null;
  scopeLabel?: string | null;
};

export type EligibleOrganization = {
  organizationId: string;
  displayName: string;
  slug: string;
  membershipRole?: string;
  membershipId?: string;
};

export type PlatformBranch = {
  id: string;
  organizationId: string;
  code: string;
  name: string;
  city?: string | null;
  region?: string | null;
  isPrimary: boolean;
  status: string;
  customerOrderingReady?: boolean;
  areaId?: string | null;
  areaName?: string | null;
  /** Retail (default) or Warehouse when Platform emits it. */
  branchType?: string | null;
};

/** ListBranches returns `id`; some harnesses still read legacy `branchId`. */
export function resolvePlatformBranchId(
  branch: Pick<PlatformBranch, "id"> & { branchId?: string | null },
): string | null {
  const resolved = branch.id?.trim() || branch.branchId?.trim() || null;
  return resolved && resolved.length > 0 ? resolved : null;
}

export type WorkspaceBindFailureReason =
  | "organization_context"
  | "branch_context"
  | "grant"
  | "access_denied";

export type SessionGrantResponse = {
  accessToken: string;
  productAccessAllowed: boolean;
  productAccessReasonCode?: string | null;
  organizationManagementAuthority?: boolean;
  mappedPosRoleCode?: string | null;
  productLocalRoleCode?: string | null;
  membershipRole?: string | null;
  /**
   * Optional feature / capability codes from the session grant (when the platform emits them).
   * Prefer these over role heuristics when present. Known override codes:
   * `store-sales-override-price`, `store-sales-override-price-unlimited`.
   */
  featureCodes?: string[] | null;
  grantedFeatureCodes?: string[] | null;
};

export async function fetchCurrentSession(): Promise<{
  status: "authenticated" | "unauthenticated" | "expired";
  session: BrowserSessionSnapshot | null;
}> {
  try {
    const body = await platformRequest<PlatformLoginWire & PlatformProblem>({
      path: AUTH_ME_PATH,
    });
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
): Promise<LoginWithPasswordResult> {
  clearPosAccessToken();
  clearPosSessionGrant();
  try {
    const body = await platformRequest<PlatformLoginWire>({
      method: "POST",
      path: AUTH_LOGIN_PATH,
      body: { usernameOrEmail, password },
      skipAntiforgery: true,
    });
    return { ok: true, session: toBrowserSessionSnapshot(body) };
  } catch (error) {
    return { ok: false, failure: buildAuthLoginFailure(error) };
  }
}

export type AccountProfileWire = {
  id: string;
  userIdentityId: string;
  accountClass: string;
  allowedScope: string;
  status: string;
};

export async function listAccountProfiles(): Promise<
  | { ok: true; profiles: AccountProfileWire[] }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  try {
    const body = await platformRequest<AccountProfileWire[]>({ path: AUTH_ACCOUNT_PROFILES_PATH });
    return { ok: true, profiles: Array.isArray(body) ? body : [] };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function selectAccountProfile(
  accountProfileId: string,
): Promise<
  | { ok: true; session: BrowserSessionSnapshot }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  try {
    const body = await platformRequest<PlatformLoginWire>({
      method: "POST",
      path: AUTH_ACCOUNT_PROFILES_SELECT_PATH,
      body: { accountProfileId },
    });
    return { ok: true, session: toBrowserSessionSnapshot(body) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function ensureAccountProfile(
  accountClass: "Personal" | "Organization",
): Promise<
  | { ok: true; profile: AccountProfileWire }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  try {
    const body = await platformRequest<AccountProfileWire>({
      method: "POST",
      path: AUTH_ACCOUNT_PROFILES_ENSURE_PATH,
      body: { accountClass },
    });
    return { ok: true, profile: body };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function logoutSession(): Promise<"logged_out" | "already_signed_out"> {
  const clearArtifacts = () => {
    clearPlatformAntiforgeryToken();
    clearPosAccessToken();
    clearPosSessionGrant();
  };

  try {
    await platformRequest<void>({ method: "POST", path: AUTH_LOGOUT_PATH });
    clearArtifacts();
    return "logged_out";
  } catch (error) {
    if (error instanceof PlatformApiError && isLogoutAlreadySignedOutError(error)) {
      clearArtifacts();
      return "already_signed_out";
    }
    throw error;
  }
}

function isLogoutAlreadySignedOutError(error: PlatformApiError): boolean {
  if (error.status === 401) {
    return true;
  }

  return (
    error.errorCode === "application.auth.session_invalid" ||
    error.errorCode === "application.auth.session_expired"
  );
}

/** Local-validation quick-login list — usernames/emails only; never returns passwords. */
export async function fetchLocalValidationIdentities(): Promise<QuickLoginIdentity[]> {
  if (!isFrontendLocalValidationMode()) {
    return [];
  }

  try {
    const enabled = await platformRequest<boolean>({ path: LOCAL_VALIDATION_ENABLED_PATH });
    if (enabled !== true) {
      return [];
    }
  } catch {
    return [];
  }

  try {
    const identities = await platformRequest<QuickLoginIdentity[]>({
      path: LOCAL_VALIDATION_IDENTITIES_PATH,
    });
    return Array.isArray(identities) ? identities : [];
  } catch {
    return [];
  }
}

export function platformProblemDetail(body: PlatformProblem | null, fallback: string): string {
  const detail = body?.detail?.trim();
  return detail && detail.length > 0 ? detail : fallback;
}

export async function listEligibleOrganizations(): Promise<
  | { ok: true; organizations: EligibleOrganization[] }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  try {
    const body = await platformRequest<unknown>({ path: AUTH_ORGANIZATIONS_PATH });
    // Runtime payloads must be an array. Non-array bodies previously threw
    // "organizations is not iterable" inside WorkspaceProvider and left the
    // GlobalRuntimeErrorHost overlay blocking the desktop shell.
    if (Array.isArray(body)) {
      return { ok: true, organizations: body as EligibleOrganization[] };
    }
    if (
      body &&
      typeof body === "object" &&
      Array.isArray((body as { organizations?: unknown }).organizations)
    ) {
      return {
        ok: true,
        organizations: (body as { organizations: EligibleOrganization[] }).organizations,
      };
    }
    return {
      ok: false,
      status: 502,
      body: {
        title: "Invalid organizations payload",
        detail: "Eligible organizations response was not an array.",
        status: 502,
      },
    };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function setOrganizationContext(
  organizationId: string,
): Promise<{ ok: true } | { ok: false; status: number; body: PlatformProblem | null }> {
  try {
    await platformRequest<void>({
      method: "PUT",
      path: AUTH_ORGANIZATION_CONTEXT_PATH,
      body: { organizationId },
    });
    return { ok: true };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function listOrganizationBranches(
  organizationId: string,
): Promise<
  | { ok: true; branches: PlatformBranch[] }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  try {
    const body = await platformRequest<PlatformBranch[]>({
      path: organizationBranchesPath(organizationId),
    });
    return { ok: true, branches: body };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function setBranchContext(
  organizationId: string,
  branchId: string,
): Promise<{ ok: true } | { ok: false; status: number; body: PlatformProblem | null }> {
  try {
    await platformRequest<void>({
      method: "PUT",
      path: organizationBranchContextPath(organizationId),
      body: { branchId },
    });
    return { ok: true };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function issueSessionGrant(
  organizationId: string,
): Promise<
  | { ok: true; grant: SessionGrantResponse }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  try {
    const raw = await platformRequest<SessionGrantResponse & Record<string, unknown>>({
      method: "POST",
      path: AUTH_TOKEN_PATH,
      body: {
        grantType: "session",
        organizationId,
        productCode: POS_PRODUCT_CODE,
      },
    });
    return { ok: true, grant: normalizeSessionGrantResponse(raw) };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

/** Prefer camelCase; accept PascalCase wire shapes from older proxies. */
function normalizeSessionGrantResponse(
  raw: SessionGrantResponse & Record<string, unknown>,
): SessionGrantResponse {
  const membershipRole =
    (typeof raw.membershipRole === "string" ? raw.membershipRole : null) ??
    (typeof raw.MembershipRole === "string" ? (raw.MembershipRole as string) : null);
  const mappedPosRoleCode =
    (typeof raw.mappedPosRoleCode === "string" ? raw.mappedPosRoleCode : null) ??
    (typeof raw.MappedPosRoleCode === "string" ? (raw.MappedPosRoleCode as string) : null);
  const productLocalRoleCode =
    (typeof raw.productLocalRoleCode === "string" ? raw.productLocalRoleCode : null) ??
    (typeof raw.ProductLocalRoleCode === "string" ? (raw.ProductLocalRoleCode as string) : null);
  const productAccessReasonCode =
    (typeof raw.productAccessReasonCode === "string" ? raw.productAccessReasonCode : null) ??
    (typeof raw.ProductAccessReasonCode === "string"
      ? (raw.ProductAccessReasonCode as string)
      : null);
  const organizationManagementAuthority =
    raw.organizationManagementAuthority === true ||
    raw.OrganizationManagementAuthority === true;
  const productAccessAllowed =
    raw.productAccessAllowed === true || raw.ProductAccessAllowed === true;

  const featureCodes = normalizeFeatureCodeList(
    raw.featureCodes ??
      raw.enabledFeatureCodes ??
      raw.FeatureCodes ??
      raw.EnabledFeatureCodes ??
      raw.grantedFeatureCodes ??
      raw.GrantedFeatureCodes,
  );

  return {
    ...raw,
    accessToken: String(raw.accessToken ?? raw.AccessToken ?? ""),
    productAccessAllowed,
    productAccessReasonCode,
    organizationManagementAuthority,
    mappedPosRoleCode,
    productLocalRoleCode,
    membershipRole,
    featureCodes: featureCodes.length > 0 ? featureCodes : null,
    grantedFeatureCodes: featureCodes.length > 0 ? featureCodes : null,
  };
}

function normalizeFeatureCodeList(value: unknown): string[] {
  if (!Array.isArray(value)) {
    return [];
  }
  return value
    .filter((item): item is string => typeof item === "string")
    .map((item) => item.trim())
    .filter((item) => item.length > 0);
}

export async function bindWorkspaceWithSessionGrant(
  organizationId: string,
  branchId: string,
): Promise<
  | { ok: true; grant: SessionGrantResponse }
  | {
      ok: false;
      reason: WorkspaceBindFailureReason;
      status: number;
      body: PlatformProblem | null;
    }
> {
  const orgContext = await setOrganizationContext(organizationId);
  if (!orgContext.ok) {
    return {
      ok: false,
      reason: "organization_context",
      status: orgContext.status,
      body: orgContext.body,
    };
  }

  const branchContext = await setBranchContext(organizationId, branchId);
  if (!branchContext.ok) {
    return {
      ok: false,
      reason: "branch_context",
      status: branchContext.status,
      body: branchContext.body,
    };
  }

  const grantResult = await issueSessionGrant(organizationId);
  if (!grantResult.ok) {
    clearPosAccessToken();
    clearPosSessionGrant();
    return { ok: false, reason: "grant", status: grantResult.status, body: grantResult.body };
  }

  if (!grantResult.grant.productAccessAllowed) {
    clearPosAccessToken();
    clearPosSessionGrant();
    return {
      ok: false,
      reason: "access_denied",
      status: 403,
      body: {
        errorCode: "application.auth.product_access_denied",
        detail: grantResult.grant.productAccessReasonCode ?? undefined,
      },
    };
  }

  setPosAccessToken(grantResult.grant.accessToken);
  setPosSessionGrant(grantResult.grant);
  return { ok: true, grant: grantResult.grant };
}

/**
 * Organization-level management bind: org context + session grant, no branch.
 * Allows Owner/OrgAdmin management authority even when ProductAccess is not for selling.
 */
export async function bindOrganizationManagementGrant(organizationId: string): Promise<
  | { ok: true; grant: SessionGrantResponse }
  | {
      ok: false;
      reason: WorkspaceBindFailureReason;
      status: number;
      body: PlatformProblem | null;
    }
> {
  const orgContext = await setOrganizationContext(organizationId);
  if (!orgContext.ok) {
    return {
      ok: false,
      reason: "organization_context",
      status: orgContext.status,
      body: orgContext.body,
    };
  }

  const grantResult = await issueSessionGrant(organizationId);
  if (!grantResult.ok) {
    clearPosAccessToken();
    clearPosSessionGrant();
    return { ok: false, reason: "grant", status: grantResult.status, body: grantResult.body };
  }

  const grant = grantResult.grant;
  const hasManagement =
    grant.organizationManagementAuthority === true ||
    grant.membershipRole?.localeCompare("OrganizationOwner", undefined, {
      sensitivity: "accent",
    }) === 0 ||
    grant.membershipRole?.localeCompare("OrganizationAdministrator", undefined, {
      sensitivity: "accent",
    }) === 0;

  if (!grant.productAccessAllowed && !hasManagement) {
    clearPosAccessToken();
    clearPosSessionGrant();
    return {
      ok: false,
      reason: "access_denied",
      status: 403,
      body: {
        errorCode: "application.auth.product_access_denied",
        detail: grant.productAccessReasonCode ?? undefined,
      },
    };
  }

  setPosAccessToken(grant.accessToken);
  setPosSessionGrant(grant);
  return { ok: true, grant };
}

/** Probe grant for destination UI without requiring ProductAccess (management may still qualify). */
export async function probeOrganizationSessionGrant(
  organizationId: string,
): Promise<
  | { ok: true; grant: SessionGrantResponse }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  const orgContext = await setOrganizationContext(organizationId);
  if (!orgContext.ok) {
    return { ok: false, status: orgContext.status, body: orgContext.body };
  }

  const grantResult = await issueSessionGrant(organizationId);
  if (!grantResult.ok) {
    return { ok: false, status: grantResult.status, body: grantResult.body };
  }

  return { ok: true, grant: grantResult.grant };
}

export type ExternalAuthProviderAvailability = "available" | "disabled" | "offline";

export function buildExternalAuthChallengeUrl(provider: "google" | "facebook", returnUrl: string): string {
  const safeReturn = encodeURIComponent(returnUrl);
  return `/platform-api/api/v1/platform/auth/external/${provider}/challenge?returnUrl=${safeReturn}`;
}

export async function probeExternalAuthProvider(
  provider: "google" | "facebook",
): Promise<ExternalAuthProviderAvailability> {
  if (typeof navigator !== "undefined" && navigator.onLine === false) {
    return "offline";
  }

  try {
    const response = await fetch(buildExternalAuthChallengeUrl(provider, `${window.location.origin}/sign-in`), {
      method: "GET",
      redirect: "manual",
      credentials: "include",
    });
    if (response.status === 404 || response.status === 403) {
      return "disabled";
    }
    if (response.status === 302 || response.status === 301 || response.status === 200) {
      return "available";
    }
    return "disabled";
  } catch {
    return "offline";
  }
}

export async function registerPersonalAccount(
  displayName: string,
  email: string,
): Promise<{ ok: true } | { ok: false; detail: string }> {
  try {
    await platformRequest<{ message?: string }>({
      method: "POST",
      path: AUTH_REGISTER_PATH,
      body: { displayName, email, publicSurface: POS_PUBLIC_SURFACE },
      skipAntiforgery: true,
    });
    return { ok: true };
  } catch (error) {
    const detail =
      error instanceof PlatformApiError
        ? (error.problem.detail ?? error.message)
        : "Registration failed. Check your connection and try again.";
    return { ok: false, detail };
  }
}

export async function requestPasswordReset(
  usernameOrEmail: string,
): Promise<{ ok: true } | { ok: false; detail: string }> {
  try {
    await platformRequest<{ message?: string }>({
      method: "POST",
      path: AUTH_FORGOT_PASSWORD_PATH,
      body: { usernameOrEmail, publicSurface: POS_PUBLIC_SURFACE },
      skipAntiforgery: true,
    });
    return { ok: true };
  } catch (error) {
    const detail =
      error instanceof PlatformApiError
        ? (error.problem.detail ?? error.message)
        : "Password reset request failed. Check your connection and try again.";
    return { ok: false, detail };
  }
}

export async function activatePersonalAccount(
  token: string,
  password: string,
): Promise<
  | { ok: true }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  try {
    await platformRequest<unknown>({
      method: "POST",
      path: AUTH_ACTIVATE_PATH,
      body: { token, password },
      skipAntiforgery: true,
    });
    return { ok: true };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    return {
      ok: false,
      status: 0,
      body: { detail: "Activation failed. Check your connection and try again." },
    };
  }
}

export async function resetPasswordWithToken(
  token: string,
  newPassword: string,
): Promise<
  | { ok: true }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  try {
    await platformRequest<unknown>({
      method: "POST",
      path: AUTH_RESET_PASSWORD_PATH,
      body: { token, newPassword },
      skipAntiforgery: true,
    });
    return { ok: true };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    return {
      ok: false,
      status: 0,
      body: { detail: "Password reset failed. Check your connection and try again." },
    };
  }
}
