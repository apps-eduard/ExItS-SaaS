import { describe, expect, it } from "vitest";
import {
  PLATFORM_API_PROXY_PREFIX,
  resolvePlatformProxyTarget,
  rewritePlatformProxyPath,
  stripSetCookieDomain,
} from "@/pwa/platform-api-proxy";

describe("platform API proxy", () => {
  it("defaults to loopback Platform and rewrites the explicit prefix", () => {
    expect(resolvePlatformProxyTarget()).toBe("http://127.0.0.1:8091");
    expect(
      rewritePlatformProxyPath(`${PLATFORM_API_PROXY_PREFIX}/api/v1/platform/auth/login`),
    ).toBe("/api/v1/platform/auth/login");
  });

  it("rejects non-loopback destinations", () => {
    expect(() => resolvePlatformProxyTarget("http://example.com:8091")).toThrow(/loopback/i);
    expect(() => resolvePlatformProxyTarget("http://10.0.2.2:8091")).toThrow(/loopback/i);
    expect(() => resolvePlatformProxyTarget("https://evil.example")).toThrow(/loopback/i);
  });

  it("strips Domain from Set-Cookie without changing HttpOnly", () => {
    const rewritten = stripSetCookieDomain(
      ".ExItS.Platform.Auth=opaque; path=/; httponly; samesite=lax; Domain=127.0.0.1",
    );
    expect(rewritten).not.toMatch(/Domain=/i);
    expect(rewritten).toMatch(/httponly/i);
  });
});
