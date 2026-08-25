import { afterEach, describe, expect, it, vi } from "vitest";
import {
  displayPlatformApiBaseUrl,
  getFrontendRuntimeStatus,
  isLocalValidationToolsEnabled,
  isPlatformApiSameOrigin,
  resolveDevLanPlatformApiBaseUrl,
  resolvePlatformApiBaseUrl,
} from "@/lib/env";

describe("resolvePlatformApiBaseUrl", () => {
  afterEach(() => {
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
    vi.unstubAllGlobals();
  });

  it("prefers runtime config over the compiled Vite value", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { platformApiBaseUrl: "http://127.0.0.1:8091/" };
    expect(resolvePlatformApiBaseUrl()).toBe("http://127.0.0.1:8091");
  });

  it("falls back to the compiled Vite value when runtime config is empty", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { platformApiBaseUrl: "  " };
    expect(resolvePlatformApiBaseUrl()).toBe("");
  });

  it("uses same-origin relative URLs when the runtime flag is true on loopback", () => {
    vi.stubGlobal("location", {
      hostname: "127.0.0.1",
      protocol: "http:",
      href: "http://127.0.0.1:8095/admin/login",
    });
    window.__EXITS_PLATFORM_ADMIN_WEB__ = {
      platformApiBaseUrl: "http://localhost:8091",
      platformApiSameOrigin: true,
    };
    expect(isPlatformApiSameOrigin()).toBe(true);
    expect(resolvePlatformApiBaseUrl()).toBe("");
    expect(displayPlatformApiBaseUrl()).toBe("(same-origin)");
  });

  it("uses Tailscale host Platform API port in DEV instead of same-origin", () => {
    vi.stubGlobal("location", {
      hostname: "100.120.79.81",
      protocol: "http:",
      href: "http://100.120.79.81:8095/admin/login",
    });
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { platformApiSameOrigin: true };
    expect(isPlatformApiSameOrigin()).toBe(false);
    expect(resolvePlatformApiBaseUrl()).toBe("http://100.120.79.81:8091");
  });

  it("does not hardcode a Tailscale address on loopback", () => {
    vi.stubGlobal("location", {
      hostname: "127.0.0.1",
      protocol: "http:",
      href: "http://127.0.0.1:8095/admin/login",
    });
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { platformApiSameOrigin: true };
    expect(resolvePlatformApiBaseUrl()).not.toMatch(/100\.\d+\.\d+\.\d+/);
    expect(getFrontendRuntimeStatus().apiBaseUrl).not.toMatch(/100\.\d+\.\d+\.\d+/);
  });
});

describe("resolveDevLanPlatformApiBaseUrl", () => {
  it("keeps loopback on Vite same-origin proxy", () => {
    expect(resolveDevLanPlatformApiBaseUrl("127.0.0.1", "http:")).toBe("");
    expect(resolveDevLanPlatformApiBaseUrl("localhost", "http:")).toBe("");
  });

  it("points Tailscale/LAN pages at Platform API :8091 on the same host", () => {
    expect(resolveDevLanPlatformApiBaseUrl("100.120.79.81", "http:")).toBe(
      "http://100.120.79.81:8091",
    );
  });
});

describe("isLocalValidationToolsEnabled", () => {
  afterEach(() => {
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
  });

  it("fails closed when the runtime object is missing", () => {
    expect(isLocalValidationToolsEnabled()).toBe(false);
  });

  it("fails closed when the flag is absent", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { platformApiBaseUrl: "http://localhost:8091" };
    expect(isLocalValidationToolsEnabled()).toBe(false);
  });

  it("fails closed when the flag is false", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: false };
    expect(isLocalValidationToolsEnabled()).toBe(false);
  });

  it("returns true only for an explicit boolean true", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
    expect(isLocalValidationToolsEnabled()).toBe(true);
  });
});

describe("getFrontendRuntimeStatus", () => {
  afterEach(() => {
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
    vi.unstubAllGlobals();
  });

  it("reports app name, mode, API, and Local Validation without secrets", () => {
    vi.stubGlobal("location", {
      hostname: "127.0.0.1",
      protocol: "http:",
      href: "http://127.0.0.1:8095/admin/login",
    });
    window.__EXITS_PLATFORM_ADMIN_WEB__ = {
      localValidationToolsEnabled: true,
      platformApiSameOrigin: true,
      buildSha: "abc1234",
    };
    const status = getFrontendRuntimeStatus();
    expect(status.app).toBe("Platform Admin React");
    expect(status.frontendMode).toBe("test");
    expect(status.buildSha).toBe("abc1234");
    expect(status.apiBaseUrl).toBe("(same-origin)");
    expect(status.localValidationToolsEnabled).toBe(true);
    expect(JSON.stringify(status)).not.toMatch(/password|secret|token/i);
  });
});
