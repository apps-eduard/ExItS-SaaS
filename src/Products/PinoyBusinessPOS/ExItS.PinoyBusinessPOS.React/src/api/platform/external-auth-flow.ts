export const PERSONAL_EXTERNAL_AUTH_RETURN_PATH = "/external-login-callback?target=personal";

const UNSAFE_RETURN = /\\|\/\/|javascript:|data:/i;

export function isSafeExternalReturnPath(returnPath: string): boolean {
  if (!returnPath.startsWith("/") || returnPath.startsWith("//")) {
    return false;
  }
  if (UNSAFE_RETURN.test(returnPath)) {
    return false;
  }
  return true;
}

export function buildExternalAuthChallengeUrl(
  provider: "google" | "facebook",
  returnUrl: string,
): string {
  const safeReturn = isSafeExternalReturnPath(returnUrl)
    ? returnUrl
    : PERSONAL_EXTERNAL_AUTH_RETURN_PATH;
  return `/platform-api/api/v1/platform/auth/external/${provider}/challenge?returnUrl=${encodeURIComponent(safeReturn)}`;
}

export function stripSessionTokenFromLocation(
  pathname: string,
  search: string,
): { sessionToken: string; nextUrl: string } {
  const params = new URLSearchParams(search.startsWith("?") ? search.slice(1) : search);
  const sessionToken = params.get("sessionToken")?.trim() ?? "";
  params.delete("sessionToken");
  const next = params.toString();
  return {
    sessionToken,
    nextUrl: `${pathname}${next.length > 0 ? `?${next}` : ""}`,
  };
}
