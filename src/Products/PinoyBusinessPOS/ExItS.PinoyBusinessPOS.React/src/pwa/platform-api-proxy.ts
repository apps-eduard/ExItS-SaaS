const DEFAULT_PLATFORM_PROXY_TARGET = "http://127.0.0.1:8091";
const ALLOWED_HOSTS = new Set(["127.0.0.1", "localhost", "::1"]);

export const PLATFORM_API_PROXY_PREFIX = "/platform-api";

function readProxyTargetEnv(): string | undefined {
  const env = (globalThis as { process?: { env?: Record<string, string | undefined> } }).process
    ?.env;
  return env?.EXITS_PLATFORM_API_PROXY_TARGET;
}

export function resolvePlatformProxyTarget(
  raw = readProxyTargetEnv() ?? DEFAULT_PLATFORM_PROXY_TARGET,
): string {
  let parsed: URL;
  try {
    parsed = new URL(raw);
  } catch {
    throw new Error("EXITS_PLATFORM_API_PROXY_TARGET must be a valid absolute URL.");
  }

  if (parsed.protocol !== "http:" && parsed.protocol !== "https:") {
    throw new Error("EXITS_PLATFORM_API_PROXY_TARGET must use http or https.");
  }

  if (!ALLOWED_HOSTS.has(parsed.hostname)) {
    throw new Error(
      "EXITS_PLATFORM_API_PROXY_TARGET must be loopback only (127.0.0.1, localhost, or ::1).",
    );
  }

  if (parsed.username || parsed.password || parsed.search || parsed.hash) {
    throw new Error("EXITS_PLATFORM_API_PROXY_TARGET must not include credentials or query.");
  }

  return parsed.origin;
}

export function rewritePlatformProxyPath(path: string): string {
  if (path === PLATFORM_API_PROXY_PREFIX) {
    return "/";
  }
  if (path.startsWith(`${PLATFORM_API_PROXY_PREFIX}/`)) {
    return path.slice(PLATFORM_API_PROXY_PREFIX.length);
  }
  return path;
}

export function stripSetCookieDomain(value: string): string {
  return value.replace(/;\s*Domain=[^;]*/gi, "");
}
