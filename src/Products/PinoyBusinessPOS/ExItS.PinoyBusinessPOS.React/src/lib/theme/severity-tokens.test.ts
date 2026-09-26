import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const rootDir = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const globalsCss = readFileSync(resolve(rootDir, "styles/globals.css"), "utf8");
const gallerySrc = readFileSync(
  resolve(rootDir, "features/ui-standards/UiStandardsButtonGallery.tsx"),
  "utf8",
);

/** Extract the first `:root, [data-theme="light"] { ... }` block (brace-balanced). */
function extractLightThemeBlock(css: string): string {
  const start = css.search(/:root,\s*\[data-theme="light"\]\s*\{/);
  expect(start).toBeGreaterThanOrEqual(0);
  let depth = 0;
  let i = css.indexOf("{", start);
  const open = i;
  for (; i < css.length; i++) {
    const ch = css[i];
    if (ch === "{") depth++;
    else if (ch === "}") {
      depth--;
      if (depth === 0) return css.slice(open + 1, i);
    }
  }
  throw new Error("unclosed light theme block");
}

/** Extract `[data-theme="dark"] { ... }` block (brace-balanced). */
function extractDarkThemeBlock(css: string): string {
  const start = css.search(/\[data-theme="dark"\]\s*\{/);
  expect(start).toBeGreaterThanOrEqual(0);
  let depth = 0;
  let i = css.indexOf("{", start);
  const open = i;
  for (; i < css.length; i++) {
    const ch = css[i];
    if (ch === "{") depth++;
    else if (ch === "}") {
      depth--;
      if (depth === 0) return css.slice(open + 1, i);
    }
  }
  throw new Error("unclosed dark theme block");
}

function readToken(block: string, name: string): string {
  const match = block.match(new RegExp(`${name.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}:\\s*([^;]+);`));
  expect(match, `missing ${name}`).toBeTruthy();
  return match![1].trim().toLowerCase();
}

const LIGHT_BASE = {
  "--exits-severity-primary": "#10b981",
  "--exits-severity-secondary": "#f1f5f9",
  "--exits-severity-success": "#22c55e",
  "--exits-severity-info": "#0ea5e9",
  "--exits-severity-warn": "#f97316",
  "--exits-severity-help": "#a855f7",
  "--exits-severity-danger": "#ef4444",
  "--exits-severity-contrast": "#020617",
} as const;

const LIGHT_FOREGROUND = {
  "--exits-severity-primary-foreground": "#ffffff",
  "--exits-severity-secondary-foreground": "#334155",
  "--exits-severity-success-foreground": "#ffffff",
  "--exits-severity-info-foreground": "#ffffff",
  "--exits-severity-warn-foreground": "#ffffff",
  "--exits-severity-help-foreground": "#ffffff",
  "--exits-severity-danger-foreground": "#ffffff",
  "--exits-severity-contrast-foreground": "#ffffff",
} as const;

const DARK_BASE = {
  "--exits-severity-primary": "#34d399",
  "--exits-severity-secondary": "#27272a",
  "--exits-severity-success": "#4ade80",
  "--exits-severity-info": "#38bdf8",
  "--exits-severity-warn": "#fb923c",
  "--exits-severity-help": "#c084fc",
  "--exits-severity-danger": "#f87171",
  "--exits-severity-contrast": "#ffffff",
} as const;

describe("Diamond LIGHT severity tokens", () => {
  const light = extractLightThemeBlock(globalsCss);
  const dark = extractDarkThemeBlock(globalsCss);

  it("defines exact Diamond LIGHT base fills", () => {
    for (const [token, hex] of Object.entries(LIGHT_BASE)) {
      expect(readToken(light, token)).toBe(hex);
    }
  });

  it("defines exact Diamond LIGHT foregrounds (secondary slate; others white)", () => {
    for (const [token, hex] of Object.entries(LIGHT_FOREGROUND)) {
      expect(readToken(light, token)).toBe(hex);
    }
  });

  it("keeps dark severity palette separate (Aura-like lighter fills)", () => {
    for (const [token, hex] of Object.entries(DARK_BASE)) {
      expect(readToken(dark, token)).toBe(hex);
    }
  });

  it("does not leak dark severity fills into the light theme block", () => {
    expect(light).not.toMatch(/--exits-severity-success:\s*#4ade80/i);
    expect(light).not.toMatch(/--exits-severity-info:\s*#38bdf8/i);
    expect(light).not.toMatch(/--exits-severity-warn:\s*#fb923c/i);
    expect(light).not.toMatch(/--exits-severity-help:\s*#c084fc/i);
    expect(light).not.toMatch(/--exits-severity-danger:\s*#f87171/i);
    expect(light).not.toMatch(/--exits-severity-secondary:\s*#27272a/i);
    expect(light).not.toMatch(/--exits-severity-contrast:\s*#ffffff/i);
  });

  it("does not leak Diamond LIGHT fills into the dark theme block", () => {
    expect(dark).not.toMatch(/--exits-severity-primary:\s*#10b981/i);
    expect(dark).not.toMatch(/--exits-severity-secondary:\s*#f1f5f9/i);
    expect(dark).not.toMatch(/--exits-severity-success:\s*#22c55e/i);
    expect(dark).not.toMatch(/--exits-severity-info:\s*#0ea5e9/i);
    expect(dark).not.toMatch(/--exits-severity-warn:\s*#f97316/i);
    expect(dark).not.toMatch(/--exits-severity-help:\s*#a855f7/i);
    expect(dark).not.toMatch(/--exits-severity-danger:\s*#ef4444/i);
    expect(dark).not.toMatch(/--exits-severity-contrast:\s*#020617/i);
  });

  it("keeps configurable app primary independent of severity primary", () => {
    expect(readToken(light, "--exits-primary")).toBe("#166534");
    expect(readToken(light, "--exits-severity-primary")).toBe("#10b981");
    expect(readToken(light, "--exits-primary")).not.toBe(readToken(light, "--exits-severity-primary"));
  });

  it("wires UI Standards gallery solids to severity CSS variables (no scattered hex)", () => {
    expect(gallerySrc).toContain("var(--exits-severity-primary)");
    expect(gallerySrc).toContain("var(--exits-severity-secondary)");
    expect(gallerySrc).toContain("var(--exits-severity-success)");
    expect(gallerySrc).toContain("var(--exits-severity-info)");
    expect(gallerySrc).toContain("var(--exits-severity-warn)");
    expect(gallerySrc).toContain("var(--exits-severity-help)");
    expect(gallerySrc).toContain("var(--exits-severity-danger)");
    expect(gallerySrc).toContain("var(--exits-severity-contrast)");
    expect(gallerySrc).toContain("var(--exits-severity-secondary-foreground)");
    expect(gallerySrc).not.toMatch(/!bg-\[#34d399\]/);
    expect(gallerySrc).not.toMatch(/!bg-\[#4ade80\]/);
    expect(gallerySrc).not.toMatch(/!bg-\[#27272a\]/);
    expect(gallerySrc).not.toMatch(/!bg-\[#ffffff\].*!text-\[#09090b\]/);
  });
});
