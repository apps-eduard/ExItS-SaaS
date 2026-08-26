import { describe, expect, it } from "vitest";
import {
  defaultUiPreferences,
  parseUiPreferences,
  UI_PREFERENCES_STORAGE_KEY,
} from "@/lib/preferences/ui-preferences";

describe("ui preferences", () => {
  it("defaults to System, English, and Balance density", () => {
    expect(defaultUiPreferences).toEqual({ theme: "system", locale: "en", density: "balance" });
    expect(parseUiPreferences(null)).toEqual(defaultUiPreferences);
  });

  it("rejects malformed storage values", () => {
    expect(parseUiPreferences("{")).toEqual(defaultUiPreferences);
    expect(parseUiPreferences(JSON.stringify({ theme: "neon", locale: "en" }))).toEqual(
      defaultUiPreferences,
    );
  });

  it("uses a POS-client storage key and never stores tokens", () => {
    expect(UI_PREFERENCES_STORAGE_KEY).toBe("exits.pos-client.ui-preferences.v1");
    expect(UI_PREFERENCES_STORAGE_KEY).not.toMatch(/token|session|auth/i);
  });

  it("accepts Philippine locales and rejects unknown ones", () => {
    expect(parseUiPreferences(JSON.stringify({ theme: "light", locale: "ceb-PH" }))).toEqual({
      theme: "light",
      locale: "ceb-PH",
      density: "balance",
    });
    expect(parseUiPreferences(JSON.stringify({ theme: "light", locale: "ar" }))).toEqual(
      defaultUiPreferences,
    );
  });

  it("defaults missing density from older storage to balance", () => {
    expect(parseUiPreferences(JSON.stringify({ theme: "dark", locale: "en" }))).toEqual({
      theme: "dark",
      locale: "en",
      density: "balance",
    });
  });

  it("accepts compact and comfort density", () => {
    expect(
      parseUiPreferences(JSON.stringify({ theme: "light", locale: "en", density: "comfort" })),
    ).toEqual({ theme: "light", locale: "en", density: "comfort" });
    expect(
      parseUiPreferences(JSON.stringify({ theme: "light", locale: "en", density: "compact" })),
    ).toEqual({ theme: "light", locale: "en", density: "compact" });
  });
});
