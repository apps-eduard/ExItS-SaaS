import { describe, expect, it } from "vitest";
import button from "../components/button/Button.module.scss?raw";
import field from "../components/field/Field.module.scss?raw";
import input from "../components/input/Input.module.scss?raw";
import select from "../components/select/Select.module.scss?raw";
import tokens from "./tokens.scss?raw";

function block(source: string, selector: string) {
  const lines = source.split(/\r?\n/);
  const start = lines.findIndex((line) => line.trim() === `${selector} {`);
  if (start < 0) throw new Error(`Missing ${selector}`);
  const vars: Record<string, string> = {};
  for (let index = start + 1; index < lines.length; index += 1) {
    if (lines[index].trim() === "}") break;
    const match = lines[index].match(/^\s*(--[\w-]+)\s*:\s*([^;]+);/);
    if (match) vars[match[1]] = match[2].trim();
  }
  return vars;
}

describe("density typography", () => {
  it("changes control, button, label, and list type with density", () => {
    const compact = block(tokens, '[data-density="compact"]');
    const standard = block(tokens, '[data-density="standard"]');
    const large = block(tokens, '[data-density="large"]');

    expect(compact["--density-control-font"]).toBe("0.875rem");
    expect(standard["--density-control-font"]).toBe("0.9375rem");
    expect(large["--density-control-font"]).toBe("1rem");
    expect(compact["--density-button-font"]).toBe("0.8125rem");
    expect(standard["--density-button-font"]).toBe("0.875rem");
    expect(large["--density-button-font"]).toBe("1rem");
    expect(compact["--density-label-font"]).toBe("0.8125rem");
    expect(standard["--density-label-font"]).toBe("0.875rem");
    expect(large["--density-label-font"]).toBe("0.9375rem");
    expect(compact["--density-list-font"]).toBe("0.8125rem");
    expect(standard["--density-list-font"]).toBe("0.875rem");
    expect(large["--density-list-font"]).toBe("0.9375rem");
    expect(new Set([compact["--density-control-font"], standard["--density-control-font"], large["--density-control-font"]]).size).toBe(3);
  });

  it("keeps page, body, and numeric type off the density scale", () => {
    expect(tokens).toContain("--type-page-size:");
    expect(tokens).toContain("--type-body-size: 1rem;");
    expect(tokens).toContain("--type-numeric-size:");
    expect(tokens).not.toContain("--type-page-size: var(--density");
    expect(tokens).not.toContain("--type-body-size: var(--density");
    expect(tokens).not.toContain("--type-numeric-size: var(--density");
  });

  it("lets controls grow with the font and keeps compact tappable on a phone", () => {
    expect(block(tokens, '[data-density="compact"]')["--control-height"]).toBe("max(36px, calc(var(--density-control-font) * 2.5))");
    expect(block(tokens, '[data-density="standard"]')["--control-height"]).toBe("max(44px, calc(var(--density-control-font) * 2.5))");
    expect(block(tokens, '[data-density="large"]')["--control-height"]).toBe("max(52px, calc(var(--density-control-font) * 2.5))");
    expect(tokens).toContain('@media (max-width: 767px)');
    expect(tokens).toContain("max(44px, calc(var(--density-control-font) * 2.5))");
  });

  it("computes different font sizes when density changes", () => {
    document.documentElement.style.fontSize = "16px";
    const compact = block(tokens, '[data-density="compact"]');
    const standard = block(tokens, '[data-density="standard"]');
    const large = block(tokens, '[data-density="large"]');
    const px = (rem: string) => {
      const node = document.createElement("span");
      node.style.fontSize = rem;
      document.body.append(node);
      const value = getComputedStyle(node).fontSize;
      node.remove();
      if (value.endsWith("rem")) return `${Number.parseFloat(value) * 16}px`;
      return value;
    };
    expect(px(compact["--density-control-font"])).toBe("14px");
    expect(px(standard["--density-control-font"])).toBe("15px");
    expect(px(large["--density-control-font"])).toBe("16px");
    expect(px(compact["--density-button-font"])).toBe("13px");
    expect(px(standard["--density-button-font"])).toBe("14px");
    expect(px(large["--density-button-font"])).toBe("16px");
    expect(px(compact["--density-label-font"])).toBe("13px");
    expect(px(large["--density-label-font"])).toBe("15px");
    expect(px(compact["--density-control-font"])).not.toBe(px(large["--density-control-font"]));
  });

  it("points input, select, button, and labels at the density tokens", () => {
    expect(input).toContain("font-size: var(--density-control-font)");
    expect(select).toContain("font-size: var(--density-control-font)");
    expect(button).toContain("font-size: var(--density-button-font)");
    expect(field).toContain("font-size: var(--density-label-font)");
  });
});
