import { describe, expect, it } from "vitest";
import {
  DEFAULT_UI_PREFERENCES,
  readUiPreferences,
  writeUiPreferences,
} from "@/lib/preferences/ui-preferences";

class MemoryStorage implements Pick<Storage, "getItem" | "setItem"> {
  private readonly values = new Map<string, string>();
  getItem(key: string): string | null {
    return this.values.get(key) ?? null;
  }
  setItem(key: string, value: string): void {
    this.values.set(key, value);
  }
}

describe("ui preferences", () => {
  it("defaults to System theme and English", () => {
    expect(DEFAULT_UI_PREFERENCES).toEqual({ theme: "system", locale: "en" });
    expect(readUiPreferences(new MemoryStorage())).toEqual(DEFAULT_UI_PREFERENCES);
  });

  it("persists theme and locale", () => {
    const storage = new MemoryStorage();
    writeUiPreferences({ theme: "dark", locale: "fil-PH" }, storage);
    expect(readUiPreferences(storage)).toEqual({ theme: "dark", locale: "fil-PH" });
  });
});
