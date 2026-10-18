import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import {
  FIELD_GLOBAL_FLOAT_SCALE,
  FIELD_GLOBAL_LABEL_SIZE,
  FIELD_GLOBAL_STACKED_LABEL_SIZE,
  FIELD_GLOBAL_TEXT_SIZE,
  SELECT_GLOBAL_BAR_DURATION,
  SELECT_GLOBAL_BAR_THICKNESS,
} from "@/features/ui-standards/field-typography-global-default";

const uiStandardsCss = [
  readFileSync(resolve(__dirname, "ui-standards.css"), "utf8"),
  readFileSync(resolve(__dirname, "exits-qfield.css"), "utf8"),
].join("\n");

function rule(selector: string): string {
  const start = uiStandardsCss.indexOf(`\n${selector} {`);
  expect(start, `missing rule ${selector}`).toBeGreaterThan(-1);
  return uiStandardsCss.slice(start, uiStandardsCss.indexOf("}", start));
}

describe("Field typography global default", () => {
  it("locks the label at 13px and the text at 14px", () => {
    expect(FIELD_GLOBAL_LABEL_SIZE).toBe("13px");
    expect(FIELD_GLOBAL_TEXT_SIZE).toBe("14px");
    expect(FIELD_GLOBAL_FLOAT_SCALE).toBe(1);
    expect(FIELD_GLOBAL_STACKED_LABEL_SIZE).toBe("14px");
  });

  it("maps the input tokens to the lock", () => {
    const input = rule(".ui-standards-qinput");
    expect(input).toContain(`--qinput-label-size: ${FIELD_GLOBAL_LABEL_SIZE};`);
    expect(input).toContain(`--qinput-float-scale: ${FIELD_GLOBAL_FLOAT_SCALE};`);
    expect(input).toContain(`font-size: ${FIELD_GLOBAL_TEXT_SIZE};`);
    expect(rule(".ui-standards-qinput__label")).toContain("font-size: var(--qinput-label-size);");
  });

  it("maps the select tokens to the lock", () => {
    const select = rule(".ui-standards-qdropdown");
    expect(select).toContain(`--qdropdown-label-size: ${FIELD_GLOBAL_LABEL_SIZE};`);
    expect(select).toContain(`font-size: ${FIELD_GLOBAL_TEXT_SIZE};`);
    expect(rule(".ui-standards-qdropdown__label")).toContain("font-size: var(--qdropdown-label-size);");
  });

  it("keeps Label above on its own static label", () => {
    expect(
      rule(".ui-standards-qinput--stacked.ui-standards-qinput--stacked .ui-standards-qinput__box > .ui-standards-qinput__label"),
    ).toContain(`font-size: ${FIELD_GLOBAL_STACKED_LABEL_SIZE};`);
    expect(rule(".ui-standards-qdropdown--stacked .ui-standards-qdropdown__field > .ui-standards-qdropdown__label")).toContain(
      `font-size: ${FIELD_GLOBAL_STACKED_LABEL_SIZE};`,
    );
  });

  it("gives every select appearance the thick open bar", () => {
    const bar = rule(".ui-standards-qdropdown__field::after");
    expect(bar).toContain(`border-bottom: ${SELECT_GLOBAL_BAR_THICKNESS} solid`);
    expect(bar).toContain(`transition-duration: ${SELECT_GLOBAL_BAR_DURATION} !important;`);
    expect(uiStandardsCss).not.toMatch(/\.ui-standards-qdropdown--[\w-]+ \.ui-standards-qdropdown__field::after\s*\{[^}]*content:\s*none/);
  });
});
