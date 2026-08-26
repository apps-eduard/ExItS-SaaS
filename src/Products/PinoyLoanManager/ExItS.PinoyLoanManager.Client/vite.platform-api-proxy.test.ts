import { afterEach, describe, expect, it } from "vitest";
import {
  createPlatformApiProxy,
  DEFAULT_PLATFORM_API_PROXY_TARGET,
  PLATFORM_API_PROXY_PREFIX,
  PLATFORM_API_PROXY_TARGET_ENV,
  resolvePlatformApiProxyTarget,
  rewritePlatformApiProxyPath,
} from "./vite.platform-api-proxy";

describe("platform API proxy", () => {
  afterEach(() => {
    delete process.env[PLATFORM_API_PROXY_TARGET_ENV];
  });

  it("defaults to loopback Platform API and strips only /platform-api", () => {
    delete process.env[PLATFORM_API_PROXY_TARGET_ENV];
    expect(resolvePlatformApiProxyTarget()).toBe(DEFAULT_PLATFORM_API_PROXY_TARGET);
    expect(rewritePlatformApiProxyPath("/platform-api/api/v1/platform/auth/login")).toBe(
      "/api/v1/platform/auth/login",
    );
    expect(rewritePlatformApiProxyPath("/platform-api")).toBe("/");
    const proxy = createPlatformApiProxy();
    expect(Object.keys(proxy)).toEqual([PLATFORM_API_PROXY_PREFIX]);
    expect(proxy[PLATFORM_API_PROXY_PREFIX]?.target).toBe(DEFAULT_PLATFORM_API_PROXY_TARGET);
  });

  it("rejects non-loopback proxy targets", () => {
    expect(() => resolvePlatformApiProxyTarget("http://example.test:8091")).toThrow(/loopback/);
    expect(() => resolvePlatformApiProxyTarget("http://10.0.2.2:8091")).toThrow(/loopback/);
    expect(() => resolvePlatformApiProxyTarget("ftp://127.0.0.1:8091")).toThrow(/http or https/);
  });

  it("accepts explicit localhost override", () => {
    expect(resolvePlatformApiProxyTarget("http://localhost:8091/ignored")).toBe(
      "http://localhost:8091",
    );
  });
});
