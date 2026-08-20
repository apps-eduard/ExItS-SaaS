import { describe, expect, it } from "vitest";
import {
  PLATFORM_API_PROXY_PREFIX,
  rewritePlatformApiProxyPath,
  resolvePlatformApiProxyTarget,
} from "./vite.platform-api-proxy";

describe("platform api proxy", () => {
  it("defaults to loopback 8091", () => {
    expect(resolvePlatformApiProxyTarget()).toBe("http://127.0.0.1:8091");
  });

  it("strips the /platform-api prefix", () => {
    expect(
      rewritePlatformApiProxyPath(`${PLATFORM_API_PROXY_PREFIX}/api/v1/platform/auth/me`),
    ).toBe("/api/v1/platform/auth/me");
    expect(rewritePlatformApiProxyPath(PLATFORM_API_PROXY_PREFIX)).toBe("/");
  });
});
