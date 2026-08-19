import {
  AUTH_LOGIN_PATH,
  AUTH_LOGOUT_PATH,
  AUTH_ME_PATH,
  LOCAL_VALIDATION_ENABLED_PATH,
  LOCAL_VALIDATION_IDENTITIES_PATH,
  SESSION_EXPIRED_ERROR_CODE,
  platformApiJson,
  toBrowserSessionSnapshot,
  type BrowserSessionSnapshot,
  type PlatformLoginWire,
  type PlatformProblem,
} from "@/api/platform-auth/browser-session";
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
  await platformApiJson(AUTH_LOGOUT_PATH, { method: "POST" });
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
