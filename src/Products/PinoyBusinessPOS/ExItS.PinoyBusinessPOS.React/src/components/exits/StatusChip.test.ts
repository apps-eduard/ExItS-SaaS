import { createElement } from "react";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { StatusChip, StatusPill } from "@/components/exits/StatusChip";

const rootDir = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const globalsCss = readFileSync(resolve(rootDir, "styles/globals.css"), "utf8");
const statusChipSource = readFileSync(
  resolve(rootDir, "components/exits/StatusChip.tsx"),
  "utf8",
);
const branchListSource = readFileSync(
  resolve(rootDir, "features/branches/BranchManagementListPage.tsx"),
  "utf8",
);

function densityBlock(mode: "compact" | "balance" | "comfort"): string {
  const match = globalsCss.match(
    new RegExp(`\\[data-density="${mode}"\\]\\s*\\{([\\s\\S]*?)\\n\\}`),
  );
  expect(match, `missing data-density=${mode}`).toBeTruthy();
  return match![1];
}

describe("slim global status chips", () => {
  it("defines separate status-chip tokens (not interactive control/chip height)", () => {
    expect(globalsCss).toContain("--exits-status-chip-height");
    expect(globalsCss).toContain("--exits-status-chip-padding-x");
    expect(globalsCss).toContain("--exits-status-chip-font-size");
    expect(globalsCss).toContain("--exits-status-chip-border-width");
    expect(globalsCss).toContain("--exits-status-chip-gap");

    expect(densityBlock("compact")).toMatch(/--exits-status-chip-height:\s*1\.375rem/);
    expect(densityBlock("balance")).toMatch(/--exits-status-chip-height:\s*1\.625rem/);
    expect(densityBlock("comfort")).toMatch(/--exits-status-chip-height:\s*1\.875rem/);

    // Interactive filter chip heights remain larger under balance.
    expect(densityBlock("balance")).toMatch(/--exits-chip-min-height:\s*2\.25rem/);
    expect(densityBlock("balance")).toMatch(/--exits-control-height:\s*2\.25rem/);
  });

  it("StatusChip consumes status tokens and drops min-h-8 / interactive chip height", () => {
    expect(statusChipSource).toContain("exits-status-chip");
    expect(statusChipSource).not.toContain("min-h-8");
    expect(statusChipSource).not.toContain("px-3");
    expect(statusChipSource).not.toContain("--exits-chip-min-height");
    expect(statusChipSource).not.toContain("--exits-control-height");
  });

  it("renders outlined tones and StatusPill alias", () => {
    for (const tone of ["success", "warning", "info", "danger", "neutral"] as const) {
      const { unmount } = render(
        createElement(StatusChip, { tone, children: tone.toUpperCase() }),
      );
      const el = screen.getByText(tone.toUpperCase());
      expect(el.className).toContain("exits-status-chip");
      expect(el.className).toContain(`exits-status-chip--${tone}`);
      unmount();
    }

    render(createElement(StatusPill, { tone: "success", children: "Available" }));
    expect(screen.getByText("Available").className).toContain("exits-status-chip--success");
  });

  it("CSS uses slim height, full pill, outlined border, and subtle tint", () => {
    const block = globalsCss.match(/\.exits-status-chip\s*\{([\s\S]*?)\n\}/)?.[1] ?? "";
    expect(block).toMatch(/height:\s*var\(--exits-status-chip-height\)/);
    expect(block).toMatch(/border-radius:\s*9999px/);
    expect(block).toMatch(/pointer-events:\s*none/);
    expect(block).not.toMatch(/cursor:\s*pointer/);

    expect(globalsCss).toMatch(
      /\.exits-status-chip--success\s*\{[\s\S]*?border-color:\s*color-mix/,
    );
    expect(globalsCss).toMatch(
      /\.exits-status-chip--success\s*\{[\s\S]*?background:\s*color-mix[\s\S]*?5%/,
    );
  });

  it("Branches Primary/Active still use shared StatusChip", () => {
    expect(branchListSource).toContain('StatusChip tone="info"');
    expect(branchListSource).toContain("StatusChip tone={statusTone(branch.status)}");
    expect(branchListSource).toContain("from \"@/components/exits/StatusChip\"");
  });
});
