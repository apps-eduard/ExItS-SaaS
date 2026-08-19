import { afterEach, describe, expect, it } from "vitest";
import { resolvePlatformApiBaseUrl } from "@/lib/env";

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
