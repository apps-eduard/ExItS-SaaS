import { describe, expect, it } from "vitest";
import { catalogs, supportedLocales, type MessageKey } from "@/i18n/messages";
import { en } from "@/i18n/locales/en";

describe("i18n message-key parity", () => {
  const keys = Object.keys(en) as MessageKey[];

  it("exposes the Philippine locale set without Arabic", () => {
    expect(supportedLocales).toEqual(["en", "fil-PH", "ceb-PH", "ilo-PH", "hil-PH"]);
    expect(supportedLocales).not.toContain("ar");
    expect(supportedLocales).not.toContain("ar-SA");
  });

  it.each(supportedLocales)("%s has every English key with a non-empty string", (locale) => {
    const catalog = catalogs[locale];
    for (const key of keys) {
      expect(catalog[key], `${locale} missing ${key}`).toEqual(expect.any(String));
      expect(catalog[key].trim().length, `${locale} empty ${key}`).toBeGreaterThan(0);
    }
    expect(Object.keys(catalog).sort()).toEqual([...keys].sort());
  });

  it("uses the approved product search placeholder in English", () => {
    expect(en["sell.searchPlaceholder"]).toBe("Search by product name, barcode, or SKU");
  });
});
