import { describe, expect, it } from "vitest";
import html from "../../index.html?raw";
import {
  DEFAULT_APPEARANCE,
  PREMIUM_STORAGE_KEYS,
  V1_UI_PREFERENCES_KEY,
  applyAppearanceAttributes,
  readAppearance,
  resolveTheme,
  writeAppearance,
} from "./appearance";

describe("premium appearance storage", () => {
  it("uses dedicated premium keys and leaves the V1 preference key untouched", () => {
    const keys = Object.values(PREMIUM_STORAGE_KEYS);
    expect(keys).toEqual([
      "exits.premium.theme",
      "exits.premium.palette",
      "exits.premium.radius",
      "exits.premium.density",
    ]);
    expect(keys).not.toContain(V1_UI_PREFERENCES_KEY);

    const storage = window.localStorage;
    storage.clear();
    storage.setItem(V1_UI_PREFERENCES_KEY, "keep");
    writeAppearance(storage, DEFAULT_APPEARANCE);
    expect(storage.getItem(V1_UI_PREFERENCES_KEY)).toBe("keep");
    expect(readAppearance(storage)).toEqual(DEFAULT_APPEARANCE);
  });

  it("ignores invalid stored values", () => {
    const storage = window.localStorage;
    storage.setItem(PREMIUM_STORAGE_KEYS.theme, "sepia");
    storage.setItem(PREMIUM_STORAGE_KEYS.palette, "glass");
    storage.setItem(PREMIUM_STORAGE_KEYS.radius, "round");
    storage.setItem(PREMIUM_STORAGE_KEYS.density, "tiny");
    expect(readAppearance(storage)).toEqual(DEFAULT_APPEARANCE);
  });

  it("resolves system theme and writes data attributes", () => {
    expect(resolveTheme("system", true)).toBe("dark");
    expect(resolveTheme("system", false)).toBe("light");
    expect(resolveTheme("light", true)).toBe("light");

    const root = document.createElement("div");
    applyAppearanceAttributes(root, { theme: "dark", palette: "rose", radius: "pill", density: "large" }, false);
    expect(root.getAttribute("data-theme")).toBe("dark");
    expect(root.getAttribute("data-palette")).toBe("rose");
    expect(root.getAttribute("data-radius")).toBe("pill");
    expect(root.getAttribute("data-density")).toBe("large");
  });

  it("keeps the boot script on the same premium keys", () => {
    for (const key of Object.values(PREMIUM_STORAGE_KEYS)) {
      expect(html).toContain(key);
    }
    expect(html).not.toContain(V1_UI_PREFERENCES_KEY);
  });
});
