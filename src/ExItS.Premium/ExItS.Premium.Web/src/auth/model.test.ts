import { describe, expect, it } from "vitest";
import {
  accountHome,
  appSurface,
  authPreviewHref,
  authSeed,
  isEmailAddress,
  passwordReady,
  readAuthScreen,
  validateForgot,
  validateLogin,
  validateRegister,
  validateReset,
} from "./model";

describe("authentication model", () => {
  it("keeps the preview route off the design-system rewrite", () => {
    expect(appSurface("/")).toBe("entry");
    expect(appSurface("/authentication")).toBe("authentication");
    expect(appSurface("/personal/home")).toBe("personal-home");
    expect(appSurface("/workspace")).toBe("workspace");
    expect(appSurface("/design-system")).toBe("design-system");
    expect(appSurface("/data-table-preview")).toBe("data-table-preview");
    expect(appSurface("/input-motion")).toBe("input-motion");
    expect(accountHome("Personal")).toBe("/personal/home");
    expect(accountHome("personal")).toBe("/personal/home");
    expect(accountHome("Organization")).toBe("/workspace");
    expect(accountHome(null)).toBe("/workspace");
    expect(appSurface("/login")).toBe("design-system");
  });

  it("reads a refreshable screen from the query string", () => {
    expect(readAuthScreen("")).toBe("login");
    expect(readAuthScreen("?screen=register")).toBe("register");
    expect(readAuthScreen("?screen=forgot")).toBe("forgot");
    expect(readAuthScreen("?screen=reset")).toBe("reset");
    expect(readAuthScreen("?screen=nope")).toBe("login");
    expect(authPreviewHref("login")).toBe("/authentication");
    expect(authPreviewHref("reset")).toBe("/authentication?screen=reset");
  });

  it("validates email, password rules, and confirmation", () => {
    expect(isEmailAddress("ada@example.com")).toBe(true);
    expect(isEmailAddress("ada@")).toBe(false);
    expect(passwordReady("ledger-1947")).toBe(true);
    expect(passwordReady("short")).toBe(false);
    expect(validateLogin({ email: "", password: "" }).email).toMatch(/email/i);
    expect(validateLogin({ email: "ada@", password: "" }).email).toMatch(/valid email/i);
    expect(validateLogin({ email: "ada@example.com", password: "x" })).toEqual({});
    expect(validateRegister({ name: "", email: "ada@", password: "", confirm: "other" })).toMatchObject({
      name: expect.any(String),
      email: expect.any(String),
      password: expect.any(String),
      confirm: expect.any(String),
    });
    expect(validateRegister({ name: "Ada", email: "ada@example.com", password: "ledger-1947", confirm: "ledger-1947" })).toEqual({});
    expect(validateForgot({ email: "ada@" }).email).toBeTruthy();
    expect(validateReset({ password: "ledger-1947", confirm: "ledger-1948" }).confirm).toMatch(/do not match/i);
    expect(authSeed("visible").reveal).toBe(true);
    expect(authSeed("empty").password).toBe("");
  });
});
