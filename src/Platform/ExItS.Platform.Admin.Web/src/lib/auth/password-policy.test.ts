import { describe, expect, it, afterEach, vi } from "vitest";
import { buildAuthNewPasswordSchema } from "@/lib/auth/password-policy";

const messages = {
  passwordRequired: "Enter your password.",
  passwordMinLength: "Password must be at least 12 characters.",
  passwordUppercase: "Password must contain an uppercase letter.",
  passwordLowercase: "Password must contain a lowercase letter.",
  passwordDigit: "Password must contain a digit.",
  passwordSpecial: "Password must contain a non-alphanumeric character.",
};

describe("buildAuthNewPasswordSchema", () => {
  afterEach(() => {
    delete window.__EXITS_PLATFORM_ADMIN_WEB__;
    vi.unstubAllEnvs();
  });

  it("accepts single-character passwords when Local Validation tools are enabled", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
    const schema = buildAuthNewPasswordSchema(messages, "Passwords do not match.", "Confirm your password.");
    expect(schema.safeParse({ password: "1", confirmPassword: "1" }).success).toBe(true);
    expect(schema.safeParse({ password: "a", confirmPassword: "a" }).success).toBe(true);
  });

  it("rejects empty passwords in Local Validation mode", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
    const schema = buildAuthNewPasswordSchema(messages, "Passwords do not match.", "Confirm your password.");
    expect(schema.safeParse({ password: "", confirmPassword: "" }).success).toBe(false);
  });

  it("rejects single-character passwords in production mode", () => {
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "");
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: false };
    const schema = buildAuthNewPasswordSchema(messages, "Passwords do not match.", "Confirm your password.");
    expect(schema.safeParse({ password: "1", confirmPassword: "1" }).success).toBe(false);
  });

  it("reads Local Validation tools at parse time, not schema build time", () => {
    vi.stubEnv("VITE_LOCAL_VALIDATION_TOOLS", "");
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: false };
    const schema = buildAuthNewPasswordSchema(messages, "Passwords do not match.", "Confirm your password.");
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
    expect(schema.safeParse({ password: "1", confirmPassword: "1" }).success).toBe(true);
  });

  it("rejects confirmation mismatches", () => {
    window.__EXITS_PLATFORM_ADMIN_WEB__ = { localValidationToolsEnabled: true };
    const schema = buildAuthNewPasswordSchema(messages, "Passwords do not match.", "Confirm your password.");
    const result = schema.safeParse({ password: "1", confirmPassword: "2" });
    expect(result.success).toBe(false);
  });
});
