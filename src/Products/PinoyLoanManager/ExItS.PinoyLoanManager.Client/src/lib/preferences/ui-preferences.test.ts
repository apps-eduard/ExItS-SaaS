import { describe, expect, it } from "vitest";
import {
  defaultUiPreferences,
  parseUiPreferences,
  UI_PREFERENCES_STORAGE_KEY,
} from "@/lib/preferences/ui-preferences";

describe("ui preferences", () => {
  it("defaults to System and English", () => {
    expect(defaultUiPreferences).toEqual({ theme: "system", locale: "en" });
    expect(parseUiPreferences(null)).toEqual(defaultUiPreferences);
  });

  it("rejects malformed storage values", () => {
    expect(parseUiPreferences("{")).toEqual(defaultUiPreferences);
    expect(parseUiPreferences(JSON.stringify({ theme: "neon", locale: "en" }))).toEqual(
      defaultUiPreferences,
    );
  });

  it("uses a PLM-specific storage key", () => {
    expect(UI_PREFERENCES_STORAGE_KEY).toBe("exits.plm-client.ui-preferences.v1");
  });
});
