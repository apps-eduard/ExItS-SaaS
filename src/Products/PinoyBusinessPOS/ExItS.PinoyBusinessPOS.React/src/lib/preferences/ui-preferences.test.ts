import { describe, expect, it } from "vitest";
import {
  applyControlShape,
  applyMotion,
  applyPrimaryColor,
  applyUiPreferences,
  defaultUiPreferences,
  parseUiPreferences,
  PRIMARY_COLOR_OPTIONS,
  UI_PREFERENCES_STORAGE_KEY,
} from "@/lib/preferences/ui-preferences";

describe("ui preferences", () => {
  it("defaults to System, English, Balance, Green, Standard, System motion", () => {
    expect(defaultUiPreferences).toEqual({
      theme: "system",
      locale: "en",
      density: "balance",
      primaryColor: "green",
      controlShape: "standard",
      motion: "system",
    });
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
      primaryColor: "green",
      controlShape: "standard",
      motion: "system",
    });
    expect(parseUiPreferences(JSON.stringify({ theme: "light", locale: "ar" }))).toEqual(
      defaultUiPreferences,
    );
  });

  it("defaults missing appearance fields from older storage", () => {
    expect(parseUiPreferences(JSON.stringify({ theme: "dark", locale: "en" }))).toEqual({
      theme: "dark",
      locale: "en",
      density: "balance",
      primaryColor: "green",
      controlShape: "standard",
      motion: "system",
    });
  });

  it("accepts compact and comfort density", () => {
    expect(
      parseUiPreferences(JSON.stringify({ theme: "light", locale: "en", density: "comfort" })),
    ).toMatchObject({ theme: "light", locale: "en", density: "comfort" });
    expect(
      parseUiPreferences(JSON.stringify({ theme: "light", locale: "en", density: "compact" })),
    ).toMatchObject({ theme: "light", locale: "en", density: "compact" });
  });

  it("accepts five primary colors and control shape / motion", () => {
    expect(PRIMARY_COLOR_OPTIONS).toEqual(["green", "blue", "violet", "orange", "rose"]);
    for (const primaryColor of PRIMARY_COLOR_OPTIONS) {
      expect(
        parseUiPreferences(
          JSON.stringify({
            theme: "light",
            locale: "en",
            density: "balance",
            primaryColor,
            controlShape: "pill",
            motion: "reduced",
          }),
        ),
      ).toMatchObject({ primaryColor, controlShape: "pill", motion: "reduced" });
    }
  });

  it("applies primary, control shape, and motion to documentElement", () => {
    applyPrimaryColor("violet");
    expect(document.documentElement.dataset.primary).toBe("violet");
    expect(document.documentElement.dataset.accent).toBe("violet");

    applyControlShape("pill");
    expect(document.documentElement.dataset.controlShape).toBe("pill");

    applyMotion("reduced");
    expect(document.documentElement.dataset.motion).toBe("reduced");

    applyUiPreferences(defaultUiPreferences);
    expect(document.documentElement.dataset.primary).toBe("green");
    expect(document.documentElement.dataset.controlShape).toBe("standard");
    expect(document.documentElement.dataset.motion).toBe("system");
  });
});
