import { ApiClientError, platformRequest } from "@/api/http";
import {
  omitSessionToken,
  readSessionSnapshot,
  type PlatformSessionSnapshot,
} from "@/auth/session-fields";

const LOGIN_PATH = "/api/v1/platform/auth/login";
const ME_PATH = "/api/v1/platform/auth/me";
const LOGOUT_PATH = "/api/v1/platform/auth/logout";

export async function fetchPlatformSession(): Promise<PlatformSessionSnapshot | null> {
  try {
    const payload = await platformRequest<unknown>({ path: ME_PATH });
    return readSessionSnapshot(payload);
  } catch (error) {
    if (error instanceof ApiClientError && (error.status === 401 || error.status === 403)) {
      return null;
    }
    throw error;
  }
}

export async function loginPlatformSession(
  usernameOrEmail: string,
  password: string,
): Promise<PlatformSessionSnapshot> {
  const raw = await platformRequest<unknown>({
    method: "POST",
    path: LOGIN_PATH,
    body: { usernameOrEmail, password },
  });
  const payload = omitSessionToken(raw);
  if (typeof raw === "object" && raw !== null) {
    const mutable = raw as Record<string, unknown>;
    delete mutable.sessionToken;
    delete mutable.SessionToken;
  }

  const fromLogin = readSessionSnapshot(payload);
  const fromCookie = await fetchPlatformSession();
  const session = fromCookie ?? fromLogin;
  if (!session) {
    throw new ApiClientError(
      "platform",
      401,
      { status: 401, errorCode: "auth.session_invalid" },
      "",
    );
  }
  return session;
}

export async function logoutPlatformSession(): Promise<void> {
  await platformRequest<void>({ method: "POST", path: LOGOUT_PATH });
}
