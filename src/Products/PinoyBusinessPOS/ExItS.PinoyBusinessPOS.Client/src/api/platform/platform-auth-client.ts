import {
  clearPlatformAntiforgeryToken,
  platformRequest,
  PlatformApiError,
} from "@/api/platform/platform-http";
import {
  AUTH_LOGIN_PATH,
  AUTH_LOGOUT_PATH,
  AUTH_ME_PATH,
  AUTH_ORGANIZATION_CONTEXT_PATH,
  AUTH_ORGANIZATIONS_PATH,
  AUTH_TOKEN_PATH,
  organizationBranchContextPath,
  organizationBranchesPath,
  POS_PRODUCT_CODE,
  SESSION_EXPIRED_ERROR_CODE,
  toBrowserSessionSnapshot,
  type BrowserSessionSnapshot,
  type PlatformLoginWire,
  type PlatformProblem,
} from "@/api/platform/browser-session";
import { clearPosAccessToken, setPosAccessToken } from "@/api/platform/pos-access-token";
import { clearPosSessionGrant, setPosSessionGrant } from "@/api/platform/pos-session-grant";

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
};

export type SessionGrantResponse = {
  accessToken: string;
  productAccessAllowed: boolean;
  productAccessReasonCode?: string | null;
  organizationManagementAuthority?: boolean;
  mappedPosRoleCode?: string | null;
  productLocalRoleCode?: string | null;
  membershipRole?: string | null;
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
): Promise<{ ok: true; session: BrowserSessionSnapshot } | { ok: false }> {
  try {
    const body = await platformRequest<PlatformLoginWire>({
      method: "POST",
      path: AUTH_LOGIN_PATH,
      body: { usernameOrEmail, password },
      skipAntiforgery: true,
    });
    return { ok: true, session: toBrowserSessionSnapshot(body) };
  } catch (error) {
    if (error instanceof PlatformApiError && error.status >= 400 && error.status < 500) {
      return { ok: false };
    }
    throw error;
  }
}

export async function logoutSession(): Promise<void> {
  await platformRequest<void>({ method: "POST", path: AUTH_LOGOUT_PATH });
  clearPlatformAntiforgeryToken();
  clearPosAccessToken();
  clearPosSessionGrant();
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
    const body = await platformRequest<EligibleOrganization[]>({ path: AUTH_ORGANIZATIONS_PATH });
    return { ok: true, organizations: body };
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
    const grant = await platformRequest<SessionGrantResponse>({
      method: "POST",
      path: AUTH_TOKEN_PATH,
      body: {
        grantType: "session",
        organizationId,
        productCode: POS_PRODUCT_CODE,
      },
    });
    return { ok: true, grant };
  } catch (error) {
    if (error instanceof PlatformApiError) {
      return { ok: false, status: error.status, body: error.problem };
    }
    throw error;
  }
}

export async function bindWorkspaceWithSessionGrant(
  organizationId: string,
  branchId: string,
): Promise<
  | { ok: true; grant: SessionGrantResponse }
  | {
      ok: false;
      reason: "context" | "grant" | "access_denied";
      status: number;
      body: PlatformProblem | null;
    }
> {
  const orgContext = await setOrganizationContext(organizationId);
  if (!orgContext.ok) {
    return { ok: false, reason: "context", status: orgContext.status, body: orgContext.body };
  }

  const branchContext = await setBranchContext(organizationId, branchId);
  if (!branchContext.ok) {
    return { ok: false, reason: "context", status: branchContext.status, body: branchContext.body };
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
