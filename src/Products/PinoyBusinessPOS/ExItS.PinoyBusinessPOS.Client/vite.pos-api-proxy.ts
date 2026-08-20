export const POS_API_PROXY_PREFIX = "/pos-api";
export const DEFAULT_POS_API_PROXY_TARGET = "http://127.0.0.1:8092";
export const POS_API_PROXY_TARGET_ENV = "EXITS_POS_API_PROXY_TARGET";

const LOOPBACK_HOSTS = new Set(["127.0.0.1", "localhost", "::1", "[::1]"]);

export function resolvePosApiProxyTarget(raw = process.env[POS_API_PROXY_TARGET_ENV]): string {
  const value = raw?.trim() || DEFAULT_POS_API_PROXY_TARGET;
  let url: URL;
  try {
    url = new URL(value);
  } catch {
    throw new Error(`${POS_API_PROXY_TARGET_ENV} must be an absolute http(s) loopback URL.`);
  }

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new Error(`${POS_API_PROXY_TARGET_ENV} must use http or https.`);
  }

  const host = url.hostname.toLowerCase();
  if (!LOOPBACK_HOSTS.has(host)) {
    throw new Error(
      `${POS_API_PROXY_TARGET_ENV} must target loopback (127.0.0.1/localhost). Received '${host}'.`,
    );
  }

  return url.origin;
}

export function rewritePosApiProxyPath(pathname: string): string {
  if (pathname === POS_API_PROXY_PREFIX) {
    return "/";
  }

  if (pathname.startsWith(`${POS_API_PROXY_PREFIX}/`)) {
    const stripped = pathname.slice(POS_API_PROXY_PREFIX.length);
    return stripped.length > 0 ? stripped : "/";
  }

  return pathname;
}

export function createPosApiProxy() {
  return {
    [POS_API_PROXY_PREFIX]: {
      target: resolvePosApiProxyTarget(),
      changeOrigin: true,
      secure: false,
      cookieDomainRewrite: "",
      rewrite: rewritePosApiProxyPath,
    },
  };
}
