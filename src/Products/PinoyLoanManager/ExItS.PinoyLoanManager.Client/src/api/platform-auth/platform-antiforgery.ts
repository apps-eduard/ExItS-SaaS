/** PWEB-20 browser CSRF contract (request-scoped memory only). */
export const PlatformAntiforgeryDefaults = {
  tokenPath: "/api/v1/platform/antiforgery/token",
  headerName: "X-XSRF-TOKEN",
} as const;

/**
 * Paths the Platform browser antiforgery middleware leaves unprotected.
 * Must stay aligned with PlatformBrowserAntiforgeryMiddleware ExemptPaths.
 */
export const PLATFORM_ANTIFORGERY_EXEMPT_PATHS = new Set<string>([
  "/api/v1/platform/auth/login",
  "/api/v1/platform/auth/register",
  "/api/v1/platform/auth/forgot-password",
  "/api/v1/platform/auth/reset-password",
  "/api/v1/platform/auth/bootstrap",
  PlatformAntiforgeryDefaults.tokenPath,
  "/api/v1/platform/auth/external/google/callback",
  "/api/v1/platform/auth/external/facebook/callback",
  "/api/v1/platform/auth/external/testing/callback",
]);

export type AntiforgeryBootstrap = {
  headerName: string;
  token: string;
};

let inMemoryAntiforgeryToken: string | null = null;
let inMemoryAntiforgeryHeaderName: string = PlatformAntiforgeryDefaults.headerName;

export function clearPlatformAntiforgeryToken(): void {
  inMemoryAntiforgeryToken = null;
  inMemoryAntiforgeryHeaderName = PlatformAntiforgeryDefaults.headerName;
}

export function peekPlatformAntiforgeryTokenForTests(): string | null {
  return inMemoryAntiforgeryToken;
}

export function isMutationMethod(method: string): boolean {
  const normalized = method.toUpperCase();
  return (
    normalized === "POST" ||
    normalized === "PUT" ||
    normalized === "PATCH" ||
    normalized === "DELETE"
  );
}

export function isAntiforgeryExemptPath(path: string): boolean {
  const normalized = path.startsWith("/") ? path : `/${path}`;
  const withoutQuery = normalized.split("?")[0] ?? normalized;
  return PLATFORM_ANTIFORGERY_EXEMPT_PATHS.has(withoutQuery);
}

export function rememberAntiforgeryBootstrap(bootstrap: AntiforgeryBootstrap): void {
  inMemoryAntiforgeryHeaderName = bootstrap.headerName || PlatformAntiforgeryDefaults.headerName;
  inMemoryAntiforgeryToken = bootstrap.token;
}

export function getRememberedAntiforgeryHeader(): {
  headerName: string;
  token: string;
} | null {
  if (!inMemoryAntiforgeryToken) {
    return null;
  }
  return {
    headerName: inMemoryAntiforgeryHeaderName,
    token: inMemoryAntiforgeryToken,
  };
}
