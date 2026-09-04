import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { describe, expect, it } from "vitest";

const globalsCss = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "../../styles/globals.css"),
  "utf8",
);

describe("exits-entity-card foundation", () => {
  it("defines reusable entity-card structure tokens and sections", () => {
    expect(globalsCss).toContain(".exits-entity-card");
    expect(globalsCss).toContain(".exits-entity-card__header");
    expect(globalsCss).toContain(".exits-entity-card__identity");
    expect(globalsCss).toContain(".exits-entity-card__badges");
    expect(globalsCss).toContain(".exits-entity-card__meta");
    expect(globalsCss).toContain(".exits-entity-card__actions");
    expect(globalsCss).toContain("--exits-entity-card-gap");
    expect(globalsCss).toContain("--exits-list-card-padding-y");
  });

  it("keeps height content-driven (no fixed card height)", () => {
    const block = globalsCss.match(/\.exits-entity-card\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";
    expect(block).toMatch(/height:\s*auto/);
    expect(block).not.toMatch(/height:\s*\d+px/);
    expect(block).not.toMatch(/min-height:\s*\d+px/);
  });

  it("does not tint the whole card for Active status", () => {
    expect(globalsCss).not.toMatch(/\.exits-entity-card--active\s*\{/);
    expect(globalsCss).not.toMatch(/branch-mgmt-card--active\s*\{/);
  });
});
