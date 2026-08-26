import { AUTH_TOKEN_PATH, POS_PRODUCT_CODE } from "@/api/platform/browser-session";
import { getPosAccessToken, setPosAccessToken } from "@/api/platform/pos-access-token";
import { platformRequest, PlatformApiError } from "@/api/platform/platform-http";

/**
 * Ensures a Bearer token for Personal buyer POS customer-order routes.
 * Prefer an existing bound-workspace token; otherwise issue a session grant
 * without product entry (UserId-only introspection for storefront / place / mine).
 */
export async function ensurePersonalBuyerPosToken(): Promise<
  { ok: true } | { ok: false; detail: string }
> {
  if (getPosAccessToken()) {
    return { ok: true };
  }

  try {
    const grant = await platformRequest<{ accessToken?: string; AccessToken?: string }>({
      method: "POST",
      path: AUTH_TOKEN_PATH,
      body: { grantType: "session" },
    });
    const token = grant.accessToken ?? grant.AccessToken;
    if (!token) {
      return { ok: false, detail: "Personal access token was empty." };
    }
    setPosAccessToken(token);
    return { ok: true };
  } catch (error) {
    // Fallback: some environments require product code without org when a single membership exists.
    try {
      const grant = await platformRequest<{ accessToken?: string; AccessToken?: string }>({
        method: "POST",
        path: AUTH_TOKEN_PATH,
        body: { grantType: "session", productCode: POS_PRODUCT_CODE },
      });
      const token = grant.accessToken ?? grant.AccessToken;
      if (!token) {
        return { ok: false, detail: "Personal access token was empty." };
      }
      setPosAccessToken(token);
      return { ok: true };
    } catch (inner) {
      const detail =
        inner instanceof PlatformApiError
          ? (inner.problem.detail ?? inner.message)
          : error instanceof PlatformApiError
            ? (error.problem.detail ?? error.message)
            : "Could not issue personal POS access token.";
      return { ok: false, detail };
    }
  }
}
