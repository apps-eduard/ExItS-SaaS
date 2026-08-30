import { describe, expect, it } from "vitest";
import { catalogs, supportedLocales, type MessageKey } from "@/i18n/messages";
import { en } from "@/i18n/locales/en";

const MOJIBAKE_PATTERNS = [/â€"/, /â†'/, /â€¦/, /ï»¿/, /\uFFFD/] as const;

const INVENTORY_MOVEMENT_SEPARATOR_KEYS = [
  "inventory.movementType.manualIncrease",
  "inventory.movementType.manualDecrease",
  "inventory.movementType.stockCountIncrease",
  "inventory.movementType.stockCountDecrease",
] as const;

const INVENTORY_ENCODING_KEYS = [
  ...INVENTORY_MOVEMENT_SEPARATOR_KEYS,
  "inventory.expiryCounts",
  "inventory.expirationTrackingOnWithWarning",
  "inventory.addStockHint",
  "inventory.untrackedHint",
  "openingStock.unitCostHelper",
  "stockCount.loadingAll",
  "stockCount.saving",
  "stockCount.loading",
  "stockCount.completing",
] as const satisfies readonly MessageKey[];

function extractPlaceholders(value: string): string[] {
  return [...value.matchAll(/\{[a-zA-Z0-9_]+\}/g)].map((match) => match[0]).sort();
}

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

  it.each(["ceb-PH", "ilo-PH", "hil-PH"] as const)(
    "%s differs from Filipino for most keys (fidelity guard)",
    (locale) => {
      const fil = catalogs["fil-PH"];
      const catalog = catalogs[locale];
      let identical = 0;
      for (const key of keys) {
        if (catalog[key] === fil[key]) {
          identical += 1;
        }
      }
      const pct = (100 * identical) / keys.length;
      expect(pct, `${locale} still ${pct.toFixed(1)}% identical to fil-PH`).toBeLessThan(35);
    },
  );
});

describe("i18n locale encoding hygiene", () => {
  const keys = Object.keys(en) as MessageKey[];

  it.each(supportedLocales)("%s has no replacement chars or common mojibake", (locale) => {
    const catalog = catalogs[locale];
    for (const key of keys) {
      const value = catalog[key];
      for (const pattern of MOJIBAKE_PATTERNS) {
        expect(value, `${locale} ${key}`).not.toMatch(pattern);
      }
    }
  });

  it.each(supportedLocales)(
    "%s preserves English placeholder tokens on inventory encoding keys",
    (locale) => {
      const catalog = catalogs[locale];
      for (const key of INVENTORY_ENCODING_KEYS) {
        expect(extractPlaceholders(catalog[key])).toEqual(extractPlaceholders(en[key]));
      }
    },
  );

  it.each(supportedLocales)(
    "%s uses em dash separators on inventory movement labels",
    (locale) => {
      const catalog = catalogs[locale];
      for (const key of INVENTORY_MOVEMENT_SEPARATOR_KEYS) {
        const value = catalog[key];
        expect(value, `${locale} ${key}`).toContain("—");
        expect(value, `${locale} ${key}`).not.toMatch(/Stock (adjustment|count) \? /);
      }
    },
  );

  it.each(supportedLocales)(
    "%s uses middle dot on inventory expiry summary labels",
    (locale) => {
      expect(catalogs[locale]["inventory.expiryCounts"]).toContain("·");
      expect(catalogs[locale]["inventory.expirationTrackingOnWithWarning"]).toContain("·");
    },
  );
});
