import { afterEach, describe, expect, it, vi } from "vitest";
import {
  displayPlatformApiBaseUrl,
  getFrontendRuntimeStatus,
  isLocalValidationToolsEnabled,
  isPlatformApiSameOrigin,
  resolvePlatformApiBaseUrl,
} from "@/lib/env";

describe("resolvePlatformApiBaseUrl", () => {
  afterEach(() => {
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
    vi.unstubAllGlobals();
  });

  it("uses same-origin when Local Validation Vite tools are enabled (localhost)", () => {
    vi.stubEnv("DEV", true);
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "true");
    vi.stubGlobal("location", {
      hostname: "127.0.0.1",
      protocol: "http:",
      href: "http://127.0.0.1:8095/admin/login",
    });
    expect(resolvePlatformApiBaseUrl()).toBe("");
    expect(displayPlatformApiBaseUrl()).toBe("(same-origin)");
  });

  it("uses same-origin when Local Validation Vite tools are enabled (Tailscale host)", () => {
    vi.stubEnv("DEV", true);
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "true");
    vi.stubGlobal("location", {
      hostname: "100.120.79.81",
      protocol: "http:",
      href: "http://100.120.79.81:8095/admin/login",
    });
    expect(resolvePlatformApiBaseUrl()).toBe("");
    expect(displayPlatformApiBaseUrl()).toBe("(same-origin)");
  });

  it("prefers runtime config over the compiled Vite value when not in LV DEV mode", () => {
    vi.stubEnv("DEV", false);
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "false");
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { platformApiBaseUrl: "http://127.0.0.1:8091/" };
    expect(resolvePlatformApiBaseUrl()).toBe("http://127.0.0.1:8091");
  });

  it("uses same-origin relative URLs when the runtime flag is true", () => {
    vi.stubEnv("DEV", false);
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "false");
    window.__EXITS_PLATFORM_ADMIN_WEB__ = {
      platformApiBaseUrl: "http://localhost:8091",
      platformApiSameOrigin: true,
    };
    expect(isPlatformApiSameOrigin()).toBe(true);
    expect(resolvePlatformApiBaseUrl()).toBe("");
    expect(displayPlatformApiBaseUrl()).toBe("(same-origin)");
  });

  it("does not hardcode a Tailscale address", () => {
    vi.stubEnv("DEV", true);
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "true");
    expect(resolvePlatformApiBaseUrl()).not.toMatch(/100\.\d+\.\d+\.\d+/);
    expect(getFrontendRuntimeStatus().apiBaseUrl).not.toMatch(/100\.\d+\.\d+\.\d+/);
  });
});

describe("isLocalValidationToolsEnabled", () => {
  afterEach(() => {
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
    vi.unstubAllEnvs();
  });

  it("fails closed when the runtime object is missing and Vite flag is unset", () => {
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "");
    expect(isLocalValidationToolsEnabled()).toBe(false);
  });

  it("fails closed when the flag is absent", () => {
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "");
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { platformApiBaseUrl: "http://localhost:8091" };
    expect(isLocalValidationToolsEnabled()).toBe(false);
  });

  it("fails closed when the flag is false", () => {
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "");
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
    vi.unstubAllEnvs();
  });

  it("reports app name, mode, API, and Local Validation without secrets", () => {
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
