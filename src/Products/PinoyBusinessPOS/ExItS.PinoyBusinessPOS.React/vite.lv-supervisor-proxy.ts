import type { ProxyOptions } from "vite";

/** Same-origin Vite proxy to loopback Local Validation supervisor (dev only). */
export const LV_SUPERVISOR_PROXY_PREFIX = "/__dev__/lv-supervisor";
export const DEFAULT_LV_SUPERVISOR_PROXY_TARGET = "http://127.0.0.1:8099";
export const LV_SUPERVISOR_PROXY_TARGET_ENV = "EXITS_LV_SUPERVISOR_PROXY_TARGET";

const LOOPBACK_HOSTS = new Set(["127.0.0.1", "localhost", "::1", "[::1]"]);

export function resolveLvSupervisorProxyTarget(
  raw = process.env[LV_SUPERVISOR_PROXY_TARGET_ENV],
): string {
  const value = raw?.trim() || DEFAULT_LV_SUPERVISOR_PROXY_TARGET;
  let url: URL;
  try {
    url = new URL(value);
  } catch {
    throw new Error(`${LV_SUPERVISOR_PROXY_TARGET_ENV} must be an absolute http(s) loopback URL.`);
  }

  if (url.protocol !== "http:" && url.protocol !== "https:") {
    throw new Error(`${LV_SUPERVISOR_PROXY_TARGET_ENV} must use http or https.`);
  }

  const host = url.hostname.toLowerCase();
  if (!LOOPBACK_HOSTS.has(host)) {
    throw new Error(
      `${LV_SUPERVISOR_PROXY_TARGET_ENV} must target loopback (127.0.0.1/localhost). Received '${host}'.`,
    );
  }

  return url.origin;
}

export function rewriteLvSupervisorProxyPath(pathname: string): string {
  if (pathname === LV_SUPERVISOR_PROXY_PREFIX) {
    return "/";
  }

  if (pathname.startsWith(`${LV_SUPERVISOR_PROXY_PREFIX}/`)) {
    return pathname.slice(LV_SUPERVISOR_PROXY_PREFIX.length) || "/";
  }

  return pathname;
}

export function createLvSupervisorProxy(): Record<string, ProxyOptions> {
  const target = resolveLvSupervisorProxyTarget();
  return {
    [LV_SUPERVISOR_PROXY_PREFIX]: {
      target,
      changeOrigin: true,
      secure: false,
      rewrite: rewriteLvSupervisorProxyPath,
      configure(proxy) {
        proxy.on("error", (_err, _req, res) => {
          if (res && "writeHead" in res && typeof res.writeHead === "function") {
            res.writeHead(502, { "Content-Type": "application/json; charset=utf-8" });
            res.end(JSON.stringify({ error: "local-validation-supervisor-unavailable" }));
          }
        });
      },
    },
  };
}
