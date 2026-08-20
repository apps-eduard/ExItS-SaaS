export const PLATFORM_API_PROXY_PREFIX = "/platform-api";
export const DEFAULT_PLATFORM_API_PROXY_TARGET = "http://127.0.0.1:8091";
export const PLATFORM_API_PROXY_TARGET_ENV = "EXITS_PLATFORM_API_PROXY_TARGET";

const LOOPBACK_HOSTS = new Set(["127.0.0.1", "localhost", "::1", "[::1]"]);

export function resolvePlatformApiProxyTarget(
  raw = process.env[PLATFORM_API_PROXY_TARGET_ENV],
): string {
  const value = raw?.trim() || DEFAULT_PLATFORM_API_PROXY_TARGET;
  let url: URL;
  try {
    url = new URL(value);
  } catch {
    throw new Error(`${PLATFORM_API_PROXY_TARGET_ENV} must be an absolute http(s) loopback URL.`);
  }

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new Error(`${PLATFORM_API_PROXY_TARGET_ENV} must use http or https.`);
  }

  const host = url.hostname.toLowerCase();
  if (!LOOPBACK_HOSTS.has(host)) {
    throw new Error(
      `${PLATFORM_API_PROXY_TARGET_ENV} must target loopback (127.0.0.1/localhost). Received '${host}'.`,
    );
  }

  return url.origin;
}

export function rewritePlatformApiProxyPath(pathname: string): string {
  if (pathname === PLATFORM_API_PROXY_PREFIX) {
    return "/";
  }

  if (pathname.startsWith(`${PLATFORM_API_PROXY_PREFIX}/`)) {
    const stripped = pathname.slice(PLATFORM_API_PROXY_PREFIX.length);
    return stripped.length > 0 ? stripped : "/";
  }

  return pathname;
}

export function createPlatformApiProxy() {
  return {
    [PLATFORM_API_PROXY_PREFIX]: {
      target: resolvePlatformApiProxyTarget(),
      changeOrigin: true,
      secure: false,
      // Keep Set-Cookie host-aligned with the browser origin (127.0.0.1 or 10.0.2.2).
      cookieDomainRewrite: "",
      rewrite: rewritePlatformApiProxyPath,
    },
  };
}
