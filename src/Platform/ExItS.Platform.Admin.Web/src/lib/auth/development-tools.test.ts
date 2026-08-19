import { afterEach, describe, expect, it } from "vitest";
import {
  areDevelopmentToolsAllowed,
  areTestUserToolsPermitted,
} from "@/lib/auth/development-tools";

describe("areDevelopmentToolsAllowed", () => {
  it.each(["development", "test", "testing", "Development", "TEST"] as const)(
    "allows explicit Development/Testing mode %s",
    (mode) => {
      expect(areDevelopmentToolsAllowed(mode)).toBe(true);
    },
  );

  it.each(["production", "staging", "preview", "qa", "uat", "unknown", "", "  "] as const)(
    "denies unrecognized or non-dev mode %s",
    (mode) => {
      expect(areDevelopmentToolsAllowed(mode)).toBe(false);
    },
  );
});

describe("areTestUserToolsPermitted", () => {
  afterEach(() => {
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
  });

  it("hides Test User tools in production when the runtime flag is absent", () => {
    expect(areTestUserToolsPermitted("production")).toBe(false);
  });

  it("hides Test User tools in production when the runtime flag is false", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: false };
    expect(areTestUserToolsPermitted("production")).toBe(false);
  });

  it("does not treat a string true as enabled", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = {
      localValidationToolsEnabled: "true" as unknown as boolean,
    };
    expect(areTestUserToolsPermitted("production")).toBe(false);
  });

  it("permits Test User tools in production only when the runtime flag is boolean true", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
    expect(areTestUserToolsPermitted("production")).toBe(true);
  });

  it("permits Test User tools in development/test without a runtime flag", () => {
    expect(areTestUserToolsPermitted("development")).toBe(true);
    expect(areTestUserToolsPermitted("test")).toBe(true);
  });

  it("does not infer permission from hostname or port", () => {
    expect(areTestUserToolsPermitted("production")).toBe(false);
  });
});
