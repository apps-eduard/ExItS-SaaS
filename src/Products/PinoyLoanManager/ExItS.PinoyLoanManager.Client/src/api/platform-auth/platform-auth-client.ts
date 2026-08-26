import {
  AUTH_ACCOUNT_PROFILES_PATH,
  AUTH_ACCOUNT_PROFILE_SELECT_PATH,
  AUTH_ACTIVATE_PATH,
  AUTH_FORGOT_PASSWORD_PATH,
  AUTH_LOGIN_PATH,
  AUTH_LOGOUT_PATH,
  AUTH_ME_PATH,
  AUTH_ORGANIZATIONS_PATH,
  AUTH_ORGANIZATION_CONTEXT_PATH,
  AUTH_PRODUCT_ACCESS_EFFECTIVE_PATH,
  AUTH_REGISTER_PATH,
  AUTH_RESET_PASSWORD_PATH,
  LOCAL_VALIDATION_ENABLED_PATH,
  LOCAL_VALIDATION_IDENTITIES_PATH,
  PLM_PUBLIC_SURFACE,
  SESSION_EXPIRED_ERROR_CODE,
  platformApiJson,
  toBrowserSessionSnapshot,
  type BrowserSessionSnapshot,
  type PlatformLoginWire,
  type PlatformProblem,
} from "@/api/platform-auth/browser-session";
import { clearPlatformAntiforgeryToken } from "@/api/platform-auth/platform-antiforgery";
import { isFrontendLocalValidationMode } from "@/api/platform-auth/local-validation-gate";

export type QuickLoginIdentity = {
  key?: string;
  username?: string;
  displayName?: string;
  email?: string;
  listLabel?: string;
};

export async function fetchCurrentSession(): Promise<{
  status: "authenticated" | "unauthenticated" | "expired";
  session: BrowserSessionSnapshot | null;
}> {
  const { status, body } = await platformApiJson<PlatformLoginWire & PlatformProblem>(AUTH_ME_PATH);
  if (status === 200 && body) {
    return { status: "authenticated", session: toBrowserSessionSnapshot(body) };
  }
  if (status === 401 && body?.errorCode === SESSION_EXPIRED_ERROR_CODE) {
    return { status: "expired", session: null };
  }
  return { status: "unauthenticated", session: null };
}

export async function loginWithPassword(
  usernameOrEmail: string,
  password: string,
): Promise<{ ok: true; session: BrowserSessionSnapshot } | { ok: false }> {
  const { status, body } = await platformApiJson<PlatformLoginWire>(AUTH_LOGIN_PATH, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ usernameOrEmail, password }),
  });
  if (status >= 200 && status < 300 && body) {
    return { ok: true, session: toBrowserSessionSnapshot(body) };
  }
  return { ok: false };
}

export async function logoutSession(): Promise<void> {
  try {
    await platformApiJson(AUTH_LOGOUT_PATH, { method: "POST" });
  } finally {
    clearPlatformAntiforgeryToken();
  }
}

export async function fetchLocalValidationIdentities(): Promise<QuickLoginIdentity[]> {
  if (!isFrontendLocalValidationMode()) {
    return [];
  }
  const enabled = await platformApiJson<boolean>(LOCAL_VALIDATION_ENABLED_PATH);
  if (enabled.status !== 200 || enabled.body !== true) {
    return [];
  }
  const identities = await platformApiJson<QuickLoginIdentity[]>(LOCAL_VALIDATION_IDENTITIES_PATH);
  if (identities.status !== 200 || !Array.isArray(identities.body)) {
    return [];
  }
  return identities.body;
}

export function platformProblemDetail(body: PlatformProblem | null, fallback: string): string {
  const detail = body?.detail?.trim();
  return detail && detail.length > 0 ? detail : fallback;
}

export async function registerPersonalAccount(
  displayName: string,
  email: string,
): Promise<{ ok: true } | { ok: false; status: number; body: PlatformProblem | null }> {
  const { status, body } = await platformApiJson<PlatformProblem>(AUTH_REGISTER_PATH, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      displayName,
      email,
      publicSurface: PLM_PUBLIC_SURFACE,
    }),
  });
  if (status >= 200 && status < 500) {
    return { ok: true };
  }
  return { ok: false, status, body };
}

export async function activatePersonalAccount(
  token: string,
  password: string,
): Promise<{ ok: true } | { ok: false; status: number; body: PlatformProblem | null }> {
  const { status, body } = await platformApiJson<PlatformProblem>(AUTH_ACTIVATE_PATH, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, password }),
  });
  if (status >= 200 && status < 300) {
    return { ok: true };
  }
  return { ok: false, status, body };
}

export async function requestPasswordReset(usernameOrEmail: string): Promise<void> {
  await platformApiJson(AUTH_FORGOT_PASSWORD_PATH, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({
      usernameOrEmail,
      publicSurface: PLM_PUBLIC_SURFACE,
    }),
  });
}

export async function resetPasswordWithToken(
  token: string,
  newPassword: string,
): Promise<{ ok: true } | { ok: false; status: number; body: PlatformProblem | null }> {
  const { status, body } = await platformApiJson<PlatformProblem>(AUTH_RESET_PASSWORD_PATH, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ token, newPassword }),
  });
  if (status >= 200 && status < 300) {
    return { ok: true };
  }
  return { ok: false, status, body };
}

export type EligibleOrganization = {
  organizationId: string;
  displayName: string;
  slug: string;
  membershipRole?: string;
  membershipId?: string;
};

export type AccountProfile = {
  id: string;
  accountClass?: string;
  status?: string;
};

export type EffectiveProductAccess = {
  allowed: boolean;
  reasonCode?: string;
  userId?: string;
  organizationId?: string;
  productCode?: string;
  subscriptionStatus?: string | null;
};

export async function listEligibleOrganizations(): Promise<
  | { ok: true; organizations: EligibleOrganization[] }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  const { status, body } = await platformApiJson<EligibleOrganization[] | PlatformProblem>(
    AUTH_ORGANIZATIONS_PATH,
  );
  if (status >= 200 && status < 300 && Array.isArray(body)) {
    return { ok: true, organizations: body };
  }
  return { ok: false, status, body: (body as PlatformProblem | null) ?? null };
}

export async function setOrganizationContext(
  organizationId: string,
): Promise<{ ok: true } | { ok: false; status: number; body: PlatformProblem | null }> {
  const { status, body } = await platformApiJson<PlatformProblem>(AUTH_ORGANIZATION_CONTEXT_PATH, {
    method: "PUT",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ organizationId }),
  });
  if (status >= 200 && status < 300) {
    return { ok: true };
  }
  return { ok: false, status, body };
}

export async function listAccountProfiles(): Promise<AccountProfile[]> {
  const { status, body } = await platformApiJson<AccountProfile[]>(AUTH_ACCOUNT_PROFILES_PATH);
  if (status >= 200 && status < 300 && Array.isArray(body)) {
    return body;
  }
  return [];
}

export async function selectAccountProfile(
  accountProfileId: string,
): Promise<{ ok: true; session: BrowserSessionSnapshot } | { ok: false }> {
  const { status, body } = await platformApiJson<PlatformLoginWire>(
    AUTH_ACCOUNT_PROFILE_SELECT_PATH,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ accountProfileId }),
    },
  );
  if (status >= 200 && status < 300 && body) {
    return { ok: true, session: toBrowserSessionSnapshot(body) };
  }
  return { ok: false };
}

export async function evaluateCurrentSessionProductAccess(
  productCode: string,
): Promise<
  | { ok: true; access: EffectiveProductAccess }
  | { ok: false; status: number; body: PlatformProblem | null }
> {
  if (/[?&](userId|organizationId)=/i.test(productCode)) {
    throw new Error("Product access must not include userId or organizationId.");
  }
  const path = `${AUTH_PRODUCT_ACCESS_EFFECTIVE_PATH}?productCode=${encodeURIComponent(productCode)}`;
  if (/[?&](userId|organizationId)=/i.test(path)) {
    throw new Error("Product access request must not bind userId or organizationId.");
  }
  const { status, body } = await platformApiJson<EffectiveProductAccess & PlatformProblem>(path);
  if (status >= 200 && status < 300 && body && typeof body.allowed === "boolean") {
    return { ok: true, access: body };
  }
  return { ok: false, status, body };
}
