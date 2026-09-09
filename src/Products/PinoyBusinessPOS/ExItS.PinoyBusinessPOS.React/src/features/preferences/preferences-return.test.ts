import { afterEach, describe, expect, it } from "vitest";
import {
  capturePreferencesReturnFrom,
  clearPreferencesReturnTo,
  isPreferencesDestination,
  isSafePreferencesReturnPath,
  preferencesNavigationState,
  rememberPreferencesReturnTo,
  resolvePreferencesReturnTo,
  takePreferencesReturnTo,
} from "@/features/preferences/preferences-return";

describe("preferences-return", () => {
  afterEach(() => {
    clearPreferencesReturnTo();
  });

  it("accepts in-app return paths and rejects preferences itself", () => {
    expect(isSafePreferencesReturnPath("/inventory")).toBe(true);
    expect(isSafePreferencesReturnPath("/inventory/abc?x=1")).toBe(true);
    expect(isSafePreferencesReturnPath("/more")).toBe(true);
    expect(isSafePreferencesReturnPath("/role/cashier")).toBe(true);
    expect(isSafePreferencesReturnPath("/settings/preferences")).toBe(false);
    expect(isSafePreferencesReturnPath("/settings/preferences?x=1")).toBe(false);
    expect(isSafePreferencesReturnPath("//evil.example")).toBe(false);
    expect(isSafePreferencesReturnPath("https://evil.example")).toBe(false);
    expect(isSafePreferencesReturnPath(null)).toBe(false);
    expect(isPreferencesDestination("/settings/preferences")).toBe(true);
    expect(isPreferencesDestination("/inventory")).toBe(false);
  });

  it("builds navigation state from the current content route", () => {
    expect(preferencesNavigationState("/inventory", "?q=1")).toEqual({
      returnTo: "/inventory?q=1",
    });
    expect(preferencesNavigationState("/settings/preferences")).toBeUndefined();
  });

  it("prefers location state, then session storage, then fallback", () => {
    rememberPreferencesReturnTo("/inventory");
    expect(resolvePreferencesReturnTo({ returnTo: "/catalog" }, "/more")).toBe("/catalog");
    expect(resolvePreferencesReturnTo({ returnTo: "/settings/preferences" }, "/more")).toBe(
      "/inventory",
    );
    expect(resolvePreferencesReturnTo(null, "/personal/more")).toBe("/inventory");
    clearPreferencesReturnTo();
    expect(resolvePreferencesReturnTo(null, "/personal/more")).toBe("/personal/more");
  });

  it("capture from sidenav path persists for close fallback", () => {
    capturePreferencesReturnFrom("/inventory/sku-1", "");
    expect(resolvePreferencesReturnTo(null, "/more")).toBe("/inventory/sku-1");
  });

  it("take clears storage after resolve", () => {
    rememberPreferencesReturnTo("/sell");
    expect(takePreferencesReturnTo(null, "/more")).toBe("/sell");
    expect(resolvePreferencesReturnTo(null, "/more")).toBe("/more");
  });
});
