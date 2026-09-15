import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import { catalogs } from "@/i18n/messages";
import { UiStandardsTablesPanel } from "@/features/ui-standards/UiStandardsTablesPanel";
import { formatUiStandardsCursorClipboard } from "@/features/ui-standards/UiStandardsCopyCommand";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: keyof typeof catalogs.en) => catalogs.en[key] ?? String(key),
  }),
}));

const alwaysOpen = {
  isOpen: () => true,
  setOpen: () => undefined,
};

function assertEveryImplementableSampleHasCopy(panelTestId: string) {
  const panel = screen.getByTestId(panelTestId);
  const implementable = panel.querySelectorAll('[data-ui-standards-sample="implementable"]');
  expect(implementable.length).toBeGreaterThan(0);
  const missing: string[] = [];
  implementable.forEach((node) => {
    if (node.getAttribute("data-has-copy") !== "true") {
      const label = node.querySelector("span")?.textContent?.trim() ?? "(unknown)";
      missing.push(label);
    }
    const copy = node.querySelector('[data-testid="ui-standards-copy-command"]');
    if (!copy) {
      const label = node.querySelector("span")?.textContent?.trim() ?? "(unknown)";
      missing.push(`${label} (no copy footer)`);
    }
  });
  expect(missing, `Missing copy on: ${missing.join(", ")}`).toEqual([]);
  return {
    implementable: implementable.length,
    explanatory: panel.querySelectorAll('[data-ui-standards-sample="explanatory"]').length,
  };
}

describe("UI Standards every-sample copy coverage", () => {
  it("covers every implementable Tables sample (locked ExitsTable reference)", () => {
    render(
      <MemoryRouter>
        <UiStandardsTablesPanel {...alwaysOpen} />
      </MemoryRouter>,
    );
    const counts = assertEveryImplementableSampleHasCopy("ui-standards-tables-section");
    expect(screen.getByTestId("ui-standards-table-demo")).toBeInTheDocument();
    expect(counts.implementable).toBeGreaterThan(15);
  });

  it("formats Cursor clipboard prompts for Table standard", () => {
    const text = formatUiStandardsCursorClipboard("Table", "EXITS TABLE + ACTIONS ON");
    expect(text).toContain("Table");
    expect(text).toContain("EXITS TABLE + ACTIONS ON");
  });

  it("keeps Cursor copy commands on the locked table demo section", () => {
    render(
      <MemoryRouter>
        <UiStandardsTablesPanel {...alwaysOpen} />
      </MemoryRouter>,
    );
    const demo = screen.getByTestId("ui-standards-table-demo");
    expect(within(demo).getAllByTestId("ui-standards-copy-command").length).toBeGreaterThan(0);
  });
});
