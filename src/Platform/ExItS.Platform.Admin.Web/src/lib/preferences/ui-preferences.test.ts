import { describe, expect, it } from "vitest";
import {
  defaultUiPreferences,
  parseUiPreferences,
  readUiPreferences,
  UI_PREFERENCES_STORAGE_KEY,
  writeUiPreferences,
} from "@/lib/preferences/ui-preferences";

describe("parseUiPreferences", () => {
  it("returns defaults for invalid or corrupt values", () => {
    expect(parseUiPreferences(null)).toEqual(defaultUiPreferences);
    expect(parseUiPreferences("nope")).toEqual(defaultUiPreferences);
    expect(parseUiPreferences({ theme: "neon", language: "xx", density: "huge" })).toEqual(
      defaultUiPreferences,
    );
  });

  it("accepts only allowed enum values", () => {
    expect(parseUiPreferences({ theme: "dark", language: "fil-PH", density: "compact" })).toEqual({
      theme: "dark",
      language: "fil-PH",
      density: "compact",
    });
    expect(parseUiPreferences({ theme: "LIGHT", language: "en", density: "balanced" }).theme).toBe(
      "system",
    );
  });
});

describe("readUiPreferences", () => {
  it("falls back when stored JSON is corrupt", () => {
    const storage = {
      getItem: () => "{not-json",
    };
    expect(readUiPreferences(storage)).toEqual(defaultUiPreferences);
  });

  it("reads a valid stored payload", () => {
    const storage = {
      getItem: (key: string) =>
        key === UI_PREFERENCES_STORAGE_KEY
          ? JSON.stringify({ theme: "light", language: "en", density: "comfortable" })
          : null,
    };
    expect(readUiPreferences(storage)).toEqual({
      theme: "light",
      language: "en",
      density: "comfortable",
    });
  });
});

describe("writeUiPreferences", () => {
  it("persists only the UI preference payload", () => {
    const written: Record<string, string> = {};
    writeUiPreferences(
      {
        setItem: (key, value) => {
          written[key] = value;
        },
      },
      { theme: "dark", language: "fil-PH", density: "balanced" },
    );
    expect(Object.keys(written)).toEqual([UI_PREFERENCES_STORAGE_KEY]);
    expect(written[UI_PREFERENCES_STORAGE_KEY]).toBeDefined();
    expect(JSON.parse(written[UI_PREFERENCES_STORAGE_KEY] ?? "")).toEqual({
      theme: "dark",
      language: "fil-PH",
      density: "balanced",
    });
  });
});
