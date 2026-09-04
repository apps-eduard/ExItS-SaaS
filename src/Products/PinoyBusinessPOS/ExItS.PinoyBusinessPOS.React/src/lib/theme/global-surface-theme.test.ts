import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";
import { applyTheme } from "@/lib/preferences/ui-preferences";

const globalsCss = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "../../styles/globals.css"),
  "utf8",
);

const REQUIRED_SEMANTIC_TOKENS = [
  "--exits-bg",
  "--exits-surface",
  "--exits-surface-elevated",
  "--exits-surface-muted",
  "--exits-border",
  "--exits-border-strong",
  "--exits-text",
  "--exits-text-muted",
  "--exits-text-subtle",
  "--exits-primary",
  "--exits-primary-hover",
  "--exits-primary-soft",
  "--exits-primary-foreground",
  "--exits-success",
  "--exits-success-soft",
  "--exits-warning",
  "--exits-warning-soft",
  "--exits-danger",
  "--exits-danger-soft",
  "--exits-ring",
  "--exits-shadow-sm",
  "--exits-shadow-md",
  "--exits-shadow-lg",
  "--exits-radius-sm",
  "--exits-radius-md",
  "--exits-radius-lg",
  "--exits-motion-fast",
  "--exits-motion-normal",
  "--exits-motion-slow",
] as const;

describe("POS global surface theme tokens", () => {
  it("defines the required semantic token family", () => {
    for (const token of REQUIRED_SEMANTIC_TOKENS) {
      expect(globalsCss, `missing ${token}`).toContain(`${token}:`);
    }
  });

  it("keeps light page background neutral (not mint-tinted)", () => {
    expect(globalsCss).toMatch(/--exits-bg:\s*#f3f4f6/);
    expect(globalsCss).not.toMatch(/\[data-theme="light"\][\s\S]{0,400}--exits-bg:\s*#f3f6f4/);
    expect(globalsCss).toMatch(/--exits-surface-muted:\s*#eceef1/);
    expect(globalsCss).toMatch(/--exits-primary-soft:\s*#eef6f1/);
  });

  it("preserves dark page/surface direction", () => {
    expect(globalsCss).toMatch(/\[data-theme="dark"\][\s\S]{0,200}--exits-bg:\s*#0e1411/);
    expect(globalsCss).toMatch(/\[data-theme="dark"\][\s\S]{0,400}--exits-surface:\s*#161e1a/);
    expect(globalsCss).toMatch(/\[data-theme="dark"\][\s\S]{0,600}--exits-primary:\s*#4ade80/);
  });

  it("exposes surface primitives and accent readiness selectors", () => {
    expect(globalsCss).toContain(".exits-card");
    expect(globalsCss).toContain(".exits-card--raised");
    expect(globalsCss).toContain(".exits-card--selected");
    expect(globalsCss).toContain(".exits-metric-surface");
    expect(globalsCss).toContain(".exits-alert-surface");
    expect(globalsCss).toContain('[data-accent="green"]');
    expect(globalsCss).toContain('[data-accent="violet"]');
    expect(globalsCss).toContain('[data-accent="blue"]');
  });

  it("zeros motion tokens under prefers-reduced-motion", () => {
    expect(globalsCss).toMatch(
      /prefers-reduced-motion:\s*reduce[\s\S]{0,200}--exits-motion-fast:\s*0ms/,
    );
    expect(globalsCss).toMatch(
      /prefers-reduced-motion:\s*reduce[\s\S]{0,280}--exits-motion-normal:\s*0ms/,
    );
    expect(globalsCss).toMatch(
      /prefers-reduced-motion:\s*reduce[\s\S]{0,360}--exits-motion-slow:\s*0ms/,
    );
  });

  it("applies theme preference to documentElement dataset", () => {
    applyTheme("light");
    expect(document.documentElement.dataset.theme).toBe("light");
    applyTheme("dark");
    expect(document.documentElement.dataset.theme).toBe("dark");
    applyTheme("system");
    expect(document.documentElement.dataset.theme).toBe("system");
  });
});
