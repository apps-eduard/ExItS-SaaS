import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  SEARCH_CLEAR_GLOBAL_COLOR,
  SEARCH_CLEAR_GLOBAL_ICON_SIZE,
  SEARCH_CLEAR_GLOBAL_INTENT,
  SEARCH_CLEAR_GLOBAL_STROKE,
} from "@/features/ui-standards/search-clear-global-default";

const rootDir = resolve(__dirname, "../..");
const globalsCss = readFileSync(resolve(rootDir, "styles/globals.css"), "utf8");
const qfieldCss = readFileSync(resolve(__dirname, "exits-qfield.css"), "utf8");

describe("Search clear global default", () => {
  it("locks primary color and bold stroke", () => {
    expect(SEARCH_CLEAR_GLOBAL_INTENT).toBe("primary");
    expect(SEARCH_CLEAR_GLOBAL_COLOR).toBe("var(--exits-primary)");
    expect(SEARCH_CLEAR_GLOBAL_STROKE).toBe(3);
    expect(SEARCH_CLEAR_GLOBAL_ICON_SIZE).toBe("1rem");
  });

  it("maps SearchField clear CSS to the lock", () => {
    expect(globalsCss).toMatch(
      new RegExp(
        `\\.exits-search-field__clear[\\s\\S]*?color:\\s*${SEARCH_CLEAR_GLOBAL_COLOR.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}`,
      ),
    );
    expect(globalsCss).toMatch(
      new RegExp(
        `\\.exits-search-field__clear svg[\\s\\S]*?stroke-width:\\s*${SEARCH_CLEAR_GLOBAL_STROKE}`,
      ),
    );
    expect(globalsCss).toMatch(
      new RegExp(
        `\\.exits-search-field__clear svg[\\s\\S]*?width:\\s*${SEARCH_CLEAR_GLOBAL_ICON_SIZE}`,
      ),
    );
  });

  it("maps qinput and dropdown search clear CSS to the lock", () => {
    expect(qfieldCss).toMatch(
      new RegExp(
        `\\.ui-standards-qinput__clear svg[\\s\\S]*?stroke-width:\\s*${SEARCH_CLEAR_GLOBAL_STROKE}`,
      ),
    );
    expect(qfieldCss).toMatch(
      new RegExp(
        `\\.ui-standards-qdropdown__search-clear-icon[\\s\\S]*?stroke-width:\\s*${SEARCH_CLEAR_GLOBAL_STROKE}`,
      ),
    );
    expect(qfieldCss).toContain(
      `.ui-standards-playground-search .ui-standards-qinput__clear`,
    );
  });
});
