import { describe, expect, it } from "vitest";
import {
  PLATFORM_API_PROXY_PREFIX,
  createPlatformApiProxy,
  rewritePlatformApiProxyPath,
  resolvePlatformApiProxyTarget,
} from "./vite.platform-api-proxy";

describe("platform api proxy", () => {
  it("defaults to loopback 8091", () => {
    expect(resolvePlatformApiProxyTarget()).toBe("http://127.0.0.1:8091");
  });

  it("keeps the /platform-api prefix for Google sign-in", () => {
    expect(
      rewritePlatformApiProxyPath(
        `${PLATFORM_API_PROXY_PREFIX}/api/v1/platform/auth/external/google/challenge`,
      ),
    ).toBe(`${PLATFORM_API_PROXY_PREFIX}/api/v1/platform/auth/external/google/challenge`);
    expect(
      rewritePlatformApiProxyPath(
        `${PLATFORM_API_PROXY_PREFIX}/api/v1/platform/auth/external/google/callback`,
      ),
    ).toBe(`${PLATFORM_API_PROXY_PREFIX}/api/v1/platform/auth/external/google/callback`);
  });

  it("strips the /platform-api prefix", () => {
    expect(
      rewritePlatformApiProxyPath(`${PLATFORM_API_PROXY_PREFIX}/api/v1/platform/auth/me`),
    ).toBe("/api/v1/platform/auth/me");
    expect(rewritePlatformApiProxyPath(PLATFORM_API_PROXY_PREFIX)).toBe("/");
  });

  it("rewrites cookie Domain so 10.0.2.2 and 127.0.0.1 share the session cookie host", () => {
    const proxy = createPlatformApiProxy()[PLATFORM_API_PROXY_PREFIX];
    expect(proxy.cookieDomainRewrite).toBe("");
    expect(proxy.changeOrigin).toBe(true);
  });
});
