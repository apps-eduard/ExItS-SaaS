import { createElement } from "react";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  applyDensity,
  defaultUiPreferences,
  parseUiPreferences,
} from "@/lib/preferences/ui-preferences";

const rootDir = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const globalsCss = readFileSync(resolve(rootDir, "styles/globals.css"), "utf8");
const buttonSource = readFileSync(resolve(rootDir, "components/ui/button.tsx"), "utf8");
const inputSource = readFileSync(resolve(rootDir, "components/ui/input.tsx"), "utf8");

function densityBlock(mode: "compact" | "balance" | "comfort"): string {
  const match = globalsCss.match(
    new RegExp(`\\[data-density="${mode}"\\]\\s*\\{([\\s\\S]*?)\\n\\}`),
  );
  expect(match, `missing data-density=${mode} block`).toBeTruthy();
  return match![1];
}

describe("POS global control density", () => {
  it("defaults density preference to balance", () => {
    expect(defaultUiPreferences.density).toBe("balance");
    expect(parseUiPreferences(null).density).toBe("balance");
    expect(parseUiPreferences(JSON.stringify({ theme: "dark", locale: "en" })).density).toBe(
      "balance",
    );
  });

  it("applies compact/balance/comfort to documentElement dataset", () => {
    applyDensity("compact");
    expect(document.documentElement.dataset.density).toBe("compact");
    applyDensity("balance");
    expect(document.documentElement.dataset.density).toBe("balance");
    applyDensity("comfort");
    expect(document.documentElement.dataset.density).toBe("comfort");
  });

  it("locks control and chip heights to 32 / 36 / 44 px", () => {
    expect(densityBlock("compact")).toMatch(/--exits-control-height:\s*2rem/);
    expect(densityBlock("compact")).toMatch(/--exits-chip-min-height:\s*2rem/);
    expect(densityBlock("balance")).toMatch(/--exits-control-height:\s*2\.25rem/);
    expect(densityBlock("balance")).toMatch(/--exits-chip-min-height:\s*2\.25rem/);
    expect(densityBlock("comfort")).toMatch(/--exits-control-height:\s*2\.75rem/);
    expect(densityBlock("comfort")).toMatch(/--exits-chip-min-height:\s*2\.75rem/);
  });

  it("keeps :root default control height at balance (36px)", () => {
    expect(globalsCss).toMatch(/--exits-control-height:\s*2\.25rem/);
    expect(globalsCss).toMatch(/--exits-chip-min-height:\s*2\.25rem/);
  });

  it("shared Button/Input consume density control height (not touch-target min)", () => {
    expect(buttonSource).toContain("h-[var(--exits-control-height)]");
    expect(buttonSource).toContain("size-[var(--exits-control-height)]");
    expect(buttonSource).not.toMatch(/default:[\s\S]{0,120}touch-target-min/);
    expect(inputSource).toContain("h-[var(--exits-control-height)]");
    expect(inputSource).not.toContain("--exits-touch-target-min");
  });

  it("variant styles do not set height — primary is not taller than outline", () => {
    const variantSection = buttonSource.slice(
      buttonSource.indexOf("variants:"),
      buttonSource.indexOf("size:"),
    );
    expect(variantSection).not.toMatch(/\bh-11\b|\bh-12\b|min-h-11|control-height/);
    expect(buttonSource).toContain("h-[var(--exits-control-height)]");
  });

  it("filter chips use density height and selected state does not change height", () => {
    expect(globalsCss).toMatch(/\.exits-chip\s*\{[\s\S]*?height:\s*var\(--exits-chip-min-height\)/);
    const activeBlock = globalsCss.match(/\.exits-chip--active\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";
    expect(activeBlock).not.toMatch(/height:|min-height:/);
  });

  it("renders Button density classes for default and icon sizes", () => {
    const { getByRole, rerender } = render(
      createElement(Button, { type: "button" }, "Add branch"),
    );
    expect(getByRole("button").className).toContain("exits-control-height");
    rerender(createElement(Button, { type: "button", size: "icon", "aria-label": "More" }, "·"));
    expect(getByRole("button").className).toContain("exits-control-height");
  });

  it("renders Input with density control height", () => {
    const { getByLabelText } = render(
      createElement(Input, { label: "Search branches", name: "q" }),
    );
    expect(getByLabelText("Search branches").className).toContain("exits-control-height");
  });
});
