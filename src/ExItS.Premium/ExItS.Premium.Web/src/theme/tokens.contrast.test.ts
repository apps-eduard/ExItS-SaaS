import { describe, expect, it } from "vitest";
import scss from "../styles/tokens.scss?raw";

function channel(value: number) {
  const scaled = value / 255;
  return scaled <= 0.04045 ? scaled / 12.92 : ((scaled + 0.055) / 1.055) ** 2.4;
}

function contrast(foreground: string, background: string) {
  const luminance = (hex: string) => {
    const value = hex.replace("#", "");
    const red = channel(Number.parseInt(value.slice(0, 2), 16));
    const green = channel(Number.parseInt(value.slice(2, 4), 16));
    const blue = channel(Number.parseInt(value.slice(4, 6), 16));
    return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
  };
  const lighter = Math.max(luminance(foreground), luminance(background));
  const darker = Math.min(luminance(foreground), luminance(background));
  return (lighter + 0.05) / (darker + 0.05);
}

function block(selector: string) {
  const lines = scss.split(/\r?\n/);
  const start = lines.findIndex((line) => line.trim() === `${selector} {`);
  if (start < 0) throw new Error(`Missing ${selector}`);
  const vars: Record<string, string> = {};
  for (let index = start + 1; index < lines.length; index += 1) {
    if (lines[index].trim() === "}") break;
    const match = lines[index].match(/^\s*(--[\w-]+)\s*:\s*([^;]+);/);
    if (match) vars[match[1]] = match[2].trim().toLowerCase();
  }
  return vars;
}

function expectTextContrast(foreground: string, background: string, label: string) {
  const ratio = contrast(foreground, background);
  expect(ratio, `${label} ${ratio.toFixed(2)}`).toBeGreaterThanOrEqual(4.5);
}

describe("premium tokens", () => {
  it("keeps soft corners, practical pill surfaces, and the density heights", () => {
    expect(block('[data-radius="soft"]')["--radius-button"]).toBe("14px");
    expect(block('[data-radius="soft"]')["--radius-control"]).toBe("12px");
    expect(block('[data-radius="soft"]')["--radius-card"]).toBe("18px");
    expect(block('[data-radius="soft"]')["--radius-dialog"]).toBe("24px");
    expect(block('[data-radius="pill"]')["--radius-card"]).toBe("22px");
    expect(block('[data-radius="pill"]')["--radius-dialog"]).toBe("28px");
    expect(block('[data-radius="pill"]')["--radius-sheet"]).toBe("20px");
    expect(block('[data-density="compact"]')["--control-height"]).toBe("36px");
    expect(block('[data-density="standard"]')["--control-height"]).toBe("44px");
    expect(block('[data-density="large"]')["--control-height"]).toBe("52px");
  });

  it("meets text contrast for surfaces, palettes, and status fills", () => {
    for (const theme of ["light", "dark"] as const) {
      const colors = block(`[data-theme="${theme}"]`);
      expectTextContrast(colors["--color-text"], colors["--color-page"], `${theme} text/page`);
      expectTextContrast(colors["--color-text"], colors["--color-surface"], `${theme} text/surface`);
      expectTextContrast(colors["--color-text"], colors["--color-surface-elevated"], `${theme} text/elevated`);
      expectTextContrast(colors["--color-text-secondary"], colors["--color-page"], `${theme} secondary`);
      expectTextContrast(colors["--color-text-caption"], colors["--color-page"], `${theme} caption`);
      expectTextContrast(colors["--color-disabled-text"], colors["--color-disabled-fill"], `${theme} disabled`);
      for (const name of ["success", "warning", "danger", "info"]) {
        expectTextContrast(colors[`--color-${name}-foreground`], colors[`--color-${name}`], `${theme} ${name}`);
        expectTextContrast(colors[`--color-${name}-foreground`], colors[`--color-${name}-hover`], `${theme} ${name} hover`);
        expectTextContrast(colors[`--color-${name}-foreground`], colors[`--color-${name}-pressed`], `${theme} ${name} pressed`);
        expectTextContrast(colors[`--color-${name}-ink`], colors["--color-page"], `${theme} ${name} ink/page`);
        expectTextContrast(colors[`--color-${name}-ink`], colors["--color-surface"], `${theme} ${name} ink/surface`);
      }
      for (const tone of ["lent", "owe", "people", "stores", "reminders"]) {
        expectTextContrast(colors[`--color-feature-${tone}-foreground`], colors[`--color-feature-${tone}`], `${theme} ${tone}`);
        expectTextContrast(colors[`--color-feature-${tone}-ink`], colors["--color-surface"], `${theme} ${tone} ink`);
      }
      for (const palette of ["teal", "blue", "violet", "orange", "rose"]) {
        const primary = block(`[data-theme="${theme}"][data-palette="${palette}"]`);
        for (const state of ["", "-hover", "-pressed"]) {
          expectTextContrast(
            primary["--color-primary-foreground"],
            primary[`--color-primary${state}`],
            `${theme} ${palette}${state || " base"}`,
          );
        }
      }
    }
  });
});
