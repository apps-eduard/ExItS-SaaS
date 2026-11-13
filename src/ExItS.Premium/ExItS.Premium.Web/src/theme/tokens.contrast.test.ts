import { describe, expect, it } from "vitest";
import { parseColorChannels, relativeLuminance } from "../playground/color";
import scss from "../styles/tokens.scss?raw";

function contrast(foreground: string, background: string) {
  const lighter = Math.max(relativeLuminance(foreground), relativeLuminance(background));
  const darker = Math.min(relativeLuminance(foreground), relativeLuminance(background));
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

function resolve(value: string, scopes: Array<Record<string, string>>): string {
  const match = value?.match(/^var\((--[\w-]+)\)$/);
  if (!match) return value;
  for (const scope of scopes) {
    if (scope[match[1]]) return resolve(scope[match[1]], scopes);
  }
  throw new Error(`Unresolved ${value}`);
}

function expectTextContrast(foreground: string, background: string, label: string) {
  expect(parseColorChannels(foreground), `${label} foreground parse`).not.toBeNull();
  expect(parseColorChannels(background), `${label} background parse`).not.toBeNull();
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
    expect(block('[data-density="compact"]')["--control-height"]).toBe("max(36px, calc(var(--density-control-font) * 2.5))");
    expect(block('[data-density="standard"]')["--control-height"]).toBe("max(44px, calc(var(--density-control-font) * 2.5))");
    expect(block('[data-density="large"]')["--control-height"]).toBe("max(52px, calc(var(--density-control-font) * 2.5))");
  });

  it("keeps the neutral scale, warning fill, OKLCH success/info, and five elevation levels", () => {
    const root = block(":root");
    expect(root["--neutral-50"]).toBe("#f5f6f8");
    expect(root["--neutral-900"]).toBe("#111827");
    expect(resolve("var(--palette-teal-700)", [root])).toBe("#0f766e");
    for (const theme of ["light", "dark"] as const) {
      const colors = block(`[data-theme="${theme}"]`);
      expect(colors["--color-warning"]).toBe("#ff5722");
      expect(colors["--color-warning-foreground"]).toBe("#1a0a00");
      expect(colors["--color-success"]).toBe("oklch(0.64 0.22 160)");
      expect(colors["--color-info"]).toBe("oklch(0.69 0.22 220)");
      expect(colors["--color-help"]).toBe("#9333ea");
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
    const root = block(":root");
    const shared = block("[data-theme]");
    for (const theme of ["light", "dark"] as const) {
      const colors = block(`[data-theme="${theme}"]`);
      const scopes = [colors, shared, root];
      const page = resolve(colors["--color-page"], scopes);
      const surface = resolve(colors["--color-surface"], scopes);
      const elevated = resolve(colors["--color-surface-elevated"], scopes);

      expectTextContrast(resolve(colors["--color-text"], scopes), page, `${theme} text/page`);
      expectTextContrast(resolve(colors["--color-text"], scopes), surface, `${theme} text/surface`);
      expectTextContrast(resolve(colors["--color-text"], scopes), elevated, `${theme} text/elevated`);
      expectTextContrast(resolve(colors["--color-text-secondary"], scopes), page, `${theme} secondary`);
      expectTextContrast(resolve(colors["--color-text-caption"], scopes), page, `${theme} caption`);
      expectTextContrast(
        resolve(colors["--color-disabled-text"], scopes),
        resolve(colors["--color-disabled-fill"], scopes),
        `${theme} disabled`,
      );

      for (const name of ["success", "warning", "danger", "info", "help"] as const) {
        expectTextContrast(
          resolve(colors[`--color-${name}-foreground`], scopes),
          resolve(colors[`--color-${name}`], scopes),
          `${theme} ${name}`,
        );
        expectTextContrast(
          resolve(colors[`--color-${name}-foreground`], scopes),
          resolve(colors[`--color-${name}-hover`], scopes),
          `${theme} ${name} hover`,
        );
        expectTextContrast(
          resolve(colors[`--color-${name}-foreground`], scopes),
          resolve(colors[`--color-${name}-pressed`], scopes),
          `${theme} ${name} pressed`,
        );
        expectTextContrast(resolve(colors[`--color-${name}-ink`], scopes), page, `${theme} ${name} ink/page`);
        expectTextContrast(resolve(colors[`--color-${name}-ink`], scopes), surface, `${theme} ${name} ink/surface`);
      }

      expectTextContrast(
        resolve(shared["--color-contrast-foreground"], [...scopes]),
        resolve(shared["--color-contrast"], [...scopes]),
        `${theme} contrast`,
      );

      for (const tone of ["lent", "owe", "people", "stores", "reminders"]) {
        expectTextContrast(
          resolve(colors[`--color-feature-${tone}-foreground`], scopes),
          resolve(colors[`--color-feature-${tone}`], scopes),
          `${theme} ${tone}`,
        );
        expectTextContrast(resolve(colors[`--color-feature-${tone}-ink`], scopes), surface, `${theme} ${tone} ink`);
      }

      for (const palette of ["teal", "blue", "violet", "orange", "rose"]) {
        const primary = block(`[data-theme="${theme}"][data-palette="${palette}"]`);
        for (const state of ["", "-hover", "-pressed"]) {
          expectTextContrast(
            resolve(primary["--color-primary-foreground"], [primary, colors, shared, root]),
            resolve(primary[`--color-primary${state}`], [primary, colors, shared, root]),
            `${theme} ${palette}${state || " base"}`,
          );
        }
      }
    }
  });
});
