import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { SOFT_GLASS_FIELD_RADIUS } from "@/features/ui-standards/soft-glass-card-global-default";
import {
  SOFT_GLASS_PRIMARY_FIELDS_COLOR,
  SOFT_GLASS_PRIMARY_FIELDS_COMMAND,
  SOFT_GLASS_PRIMARY_FIELDS_INPUT_CLEARABLE,
  SOFT_GLASS_PRIMARY_FIELDS_LABEL_DURATION,
  SOFT_GLASS_PRIMARY_FIELDS_LABEL_LIFT,
  SOFT_GLASS_PRIMARY_FIELDS_MENU_BORDER,
  SOFT_GLASS_PRIMARY_FIELDS_OPTION_RADIUS,
  SOFT_GLASS_PRIMARY_FIELDS_SELECTED_EDGE,
  SOFT_GLASS_PRIMARY_FIELDS_TITLE,
  SOFT_GLASS_PRIMARY_FIELDS_WIDTH,
} from "@/features/ui-standards/soft-glass-primary-fields-global-default";
import { TABLE_GLOBAL_SOFT_GLASS_SEARCH_RADIUS } from "@/features/ui-standards/table-global-default";

const css = readFileSync(resolve(__dirname, "ui-standards.css"), "utf8");
const en = readFileSync(resolve(__dirname, "../../i18n/locales/en.ts"), "utf8");

describe("Soft Glass primary fields global default", () => {
  it("locks the card title and recipe", () => {
    expect(SOFT_GLASS_PRIMARY_FIELDS_COMMAND).toBe("ext soft-glass primary global-default");
    expect(SOFT_GLASS_PRIMARY_FIELDS_TITLE).toBe("Soft Glass · Primary, global default");
    expect(SOFT_GLASS_PRIMARY_FIELDS_COLOR).toBe("var(--exits-primary)");
    expect(SOFT_GLASS_PRIMARY_FIELDS_WIDTH).toBe("18rem");
    expect(SOFT_GLASS_PRIMARY_FIELDS_LABEL_DURATION).toBe("0.45s");
    expect(SOFT_GLASS_PRIMARY_FIELDS_LABEL_LIFT).toBe("calc(-50% - 0.85rem)");
    expect(SOFT_GLASS_PRIMARY_FIELDS_MENU_BORDER).toBe("2px");
    expect(SOFT_GLASS_PRIMARY_FIELDS_OPTION_RADIUS).toBe("3px");
    expect(SOFT_GLASS_PRIMARY_FIELDS_SELECTED_EDGE).toBe("3px");
    expect(SOFT_GLASS_PRIMARY_FIELDS_INPUT_CLEARABLE).toBe(true);
    expect(SOFT_GLASS_FIELD_RADIUS).toBe("0.6875rem");
    expect(TABLE_GLOBAL_SOFT_GLASS_SEARCH_RADIUS).toBe("0.75rem");
  });

  it("keeps the showcase title and CSS on the lock", () => {
    expect(en).toContain(`"uiStandards.inputSoftGlassPrimaryTitle": "${SOFT_GLASS_PRIMARY_FIELDS_TITLE}"`);
    expect(css).toContain(".ui-standards-soft-glass-primary-fields");
    expect(css).toContain(`--sg-primary: ${SOFT_GLASS_PRIMARY_FIELDS_COLOR}`);
    expect(css).toContain(`width: ${SOFT_GLASS_PRIMARY_FIELDS_WIDTH}`);
    expect(css).toContain(`border-radius: ${TABLE_GLOBAL_SOFT_GLASS_SEARCH_RADIUS} !important`);
    expect(css).toContain(`transition-duration: ${SOFT_GLASS_PRIMARY_FIELDS_LABEL_DURATION} !important`);
    expect(css).toContain(`translateY(${SOFT_GLASS_PRIMARY_FIELDS_LABEL_LIFT})`);
    expect(css).toContain(`border-width: ${SOFT_GLASS_PRIMARY_FIELDS_MENU_BORDER} !important`);
    expect(css).toContain(`border-radius: ${SOFT_GLASS_PRIMARY_FIELDS_OPTION_RADIUS}`);
    expect(css).toContain(`inset ${SOFT_GLASS_PRIMARY_FIELDS_SELECTED_EDGE} 0 0 var(--sg-primary)`);
    const rollLabel = css.slice(css.indexOf(".ui-standards-soft-glass-roll-card .ui-standards-qinput__label"));
    expect(rollLabel.startsWith(".ui-standards-soft-glass-roll-card .ui-standards-qinput__label")).toBe(true);
    expect(rollLabel.slice(0, 900)).toContain(`transition-duration: ${SOFT_GLASS_PRIMARY_FIELDS_LABEL_DURATION} !important`);
    expect(rollLabel.slice(0, 900)).toContain(`translateY(${SOFT_GLASS_PRIMARY_FIELDS_LABEL_LIFT})`);
    expect(rollLabel.slice(0, 900)).toContain("background: transparent !important");
  });
});
