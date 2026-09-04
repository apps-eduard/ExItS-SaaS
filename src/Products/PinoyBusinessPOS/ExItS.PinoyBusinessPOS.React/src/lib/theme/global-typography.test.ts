import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const rootDir = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const globalsCss = readFileSync(resolve(rootDir, "styles/globals.css"), "utf8");
const buttonSource = readFileSync(resolve(rootDir, "components/ui/button.tsx"), "utf8");
const pageHeaderSource = readFileSync(
  resolve(rootDir, "components/exits/PageHeader.tsx"),
  "utf8",
);
const emptyStateSource = readFileSync(
  resolve(rootDir, "components/exits/EmptyState.tsx"),
  "utf8",
);
const inputSource = readFileSync(resolve(rootDir, "components/ui/input.tsx"), "utf8");

describe("POS global typography hierarchy", () => {
  it("keeps the existing professional font stack", () => {
    expect(globalsCss).toMatch(
      /--exits-font-sans:\s*"IBM Plex Sans",\s*"Source Sans 3",\s*system-ui,\s*sans-serif/,
    );
  });

  it("defines semantic font-weight tokens", () => {
    expect(globalsCss).toMatch(/--exits-font-weight-regular:\s*400/);
    expect(globalsCss).toMatch(/--exits-font-weight-medium:\s*500/);
    expect(globalsCss).toMatch(/--exits-font-weight-semibold:\s*600/);
    expect(globalsCss).toMatch(/--exits-font-weight-bold:\s*700/);
  });

  it("defines leading tokens and semantic type primitives", () => {
    expect(globalsCss).toContain("--exits-leading-tight:");
    expect(globalsCss).toContain("--exits-leading-normal:");
    expect(globalsCss).toContain("--exits-leading-relaxed:");
    expect(globalsCss).toContain(".exits-type-page-title");
    expect(globalsCss).toContain(".exits-type-section-title");
    expect(globalsCss).toContain(".exits-type-card-title");
    expect(globalsCss).toContain(".exits-type-label");
    expect(globalsCss).toContain(".exits-type-body");
    expect(globalsCss).toContain(".exits-type-muted");
    expect(globalsCss).toContain(".exits-type-kpi");
    expect(globalsCss).toContain(".exits-type-hero-kpi");
  });

  it("defaults shared Button to medium weight", () => {
    expect(buttonSource).toContain("font-medium");
    expect(buttonSource).not.toMatch(/buttonVariants[\s\S]*?font-semibold/);
    expect(buttonSource).not.toMatch(/buttonVariants[\s\S]*?font-bold/);
  });

  it("defaults StatusChip to medium weight", () => {
    const block = globalsCss.match(/\.exits-status-chip\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";
    expect(block).toMatch(/font-weight:\s*var\(--exits-font-weight-medium\)/);
  });

  it("uses semibold page / section / card titles and regular body/muted", () => {
    expect(pageHeaderSource).toContain("exits-type-page-title");
    expect(pageHeaderSource).not.toContain("font-bold");

    const pageTitle = globalsCss.match(/\.exits-type-page-title\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";
    const sectionTitle =
      globalsCss.match(/\.exits-type-section-title\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";
    const cardTitle = globalsCss.match(/\.exits-type-card-title\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";
    const body = globalsCss.match(/\.exits-type-body\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";
    const muted = globalsCss.match(/\.exits-type-muted\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";
    const heroKpi = globalsCss.match(/\.exits-type-hero-kpi\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";

    expect(pageTitle).toMatch(/font-weight:\s*var\(--exits-font-weight-semibold\)/);
    expect(sectionTitle).toMatch(/font-weight:\s*var\(--exits-font-weight-semibold\)/);
    expect(cardTitle).toMatch(/font-weight:\s*var\(--exits-font-weight-semibold\)/);
    expect(body).toMatch(/font-weight:\s*var\(--exits-font-weight-regular\)/);
    expect(muted).toMatch(/font-weight:\s*var\(--exits-font-weight-regular\)/);
    expect(heroKpi).toMatch(/font-weight:\s*var\(--exits-font-weight-semibold\)/);
  });

  it("keeps empty-state title medium and detail muted/regular", () => {
    expect(emptyStateSource).toContain("exits-type-label");
    expect(emptyStateSource).toContain("exits-type-muted");
    expect(emptyStateSource).not.toContain("font-semibold");
  });

  it("uses medium field labels", () => {
    expect(inputSource).toContain("exits-type-label");
    expect(inputSource).not.toContain("font-semibold");
  });

  it("does not change density or accent theme values", () => {
    expect(globalsCss).toMatch(/\[data-density="compact"\][\s\S]*?--exits-control-height:\s*2rem/);
    expect(globalsCss).toMatch(
      /\[data-density="balance"\][\s\S]*?--exits-control-height:\s*2\.25rem/,
    );
    expect(globalsCss).toMatch(
      /\[data-density="comfort"\][\s\S]*?--exits-control-height:\s*2\.75rem/,
    );
    expect(globalsCss).toMatch(/--exits-bg:\s*#f3f4f6/);
    expect(globalsCss).toContain('[data-accent="green"]');
  });
});
