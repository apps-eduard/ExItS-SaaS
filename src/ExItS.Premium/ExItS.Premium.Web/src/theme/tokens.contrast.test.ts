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

function resolve(value: string, root = block(":root")): string {
  const match = value?.match(/^var\((--[\w-]+)\)$/);
  if (!match) return value;
  return resolve(root[match[1]], root);
}

function expectTextContrast(foreground: string, background: string, label: string) {
  const ratio = contrast(resolve(foreground), resolve(background));
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
    expect(block('[data-density="compact"]')["--control-height"]).toBe("max(36px, calc(var(--density-control-font) * 2.5))");
    expect(block('[data-density="standard"]')["--control-height"]).toBe("max(44px, calc(var(--density-control-font) * 2.5))");
    expect(block('[data-density="large"]')["--control-height"]).toBe("max(52px, calc(var(--density-control-font) * 2.5))");
  });

  it("keeps the neutral scale, warning fill, and five elevation levels", () => {
    const root = block(":root");
    expect(root["--neutral-50"]).toBe("#f5f6f8");
    expect(root["--neutral-900"]).toBe("#111827");
    expect(resolve("var(--palette-teal-700)", root)).toBe("#0f766e");
    for (const theme of ["light", "dark"] as const) {
      const colors = block(`[data-theme="${theme}"]`);
      expect(colors["--color-warning"]).toBe("#ff5722");
      expect(colors["--color-warning-foreground"]).toBe("#1a0a00");
      expect(colors["--elevation-0"]).toBe("none");
      expect(colors["--elevation-1"]).toBeTruthy();
      expect(colors["--elevation-2"]).toBeTruthy();
      expect(colors["--elevation-3"]).toBeTruthy();
      expect(colors["--elevation-4"]).toBeTruthy();
      expect(colors["--shadow-1"]).toBe("var(--elevation-1)");
      expect(colors["--shadow-2"]).toBe("var(--elevation-3)");
      expect(colors["--shadow-3"]).toBe("var(--elevation-4)");
    }
    expect(block('[data-theme="light"]')["--elevation-4"]).not.toBe(block('[data-theme="dark"]')["--elevation-4"]);
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
