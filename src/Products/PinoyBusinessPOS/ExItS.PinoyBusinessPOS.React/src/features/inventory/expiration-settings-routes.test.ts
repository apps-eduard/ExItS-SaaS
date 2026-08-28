import { describe, expect, it } from "vitest";
import {
  expirationSettingsPath,
  parseExpirationSettingsFocus,
} from "@/features/inventory/expiration-settings-routes";

describe("expiration-settings-routes", () => {
  it("builds paths with optional focus", () => {
    expect(expirationSettingsPath("abc")).toBe("/inventory/abc/expiration");
    expect(expirationSettingsPath("abc", "assign")).toBe(
      "/inventory/abc/expiration?focus=assign",
    );
    expect(expirationSettingsPath("abc", "warning")).toBe(
      "/inventory/abc/expiration?focus=warning",
    );
  });

  it("parses focus from search string", () => {
    expect(parseExpirationSettingsFocus("?focus=assign")).toBe("assign");
    expect(parseExpirationSettingsFocus("?focus=warning")).toBe("warning");
    expect(parseExpirationSettingsFocus("?focus=other")).toBeNull();
    expect(parseExpirationSettingsFocus("")).toBeNull();
  });
});
