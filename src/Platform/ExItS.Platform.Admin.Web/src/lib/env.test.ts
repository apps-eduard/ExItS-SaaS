import { afterEach, describe, expect, it } from "vitest";
import { isLocalValidationToolsEnabled, resolvePlatformApiBaseUrl } from "@/lib/env";

describe("resolvePlatformApiBaseUrl", () => {
  afterEach(() => {
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
  });

  it("prefers runtime config over the compiled Vite value", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { platformApiBaseUrl: "http://127.0.0.1:8091/" };
    expect(resolvePlatformApiBaseUrl()).toBe("http://127.0.0.1:8091");
  });

  it("falls back to the compiled Vite value when runtime config is empty", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { platformApiBaseUrl: "  " };
    expect(resolvePlatformApiBaseUrl()).toBe("");
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
