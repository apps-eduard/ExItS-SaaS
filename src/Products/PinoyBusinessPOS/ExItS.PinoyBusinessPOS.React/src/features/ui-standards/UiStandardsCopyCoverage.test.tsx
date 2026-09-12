import { MemoryRouter } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { render, screen, within } from "@testing-library/react";
import { catalogs } from "@/i18n/messages";
import { UiStandardsButtonsPanel } from "@/features/ui-standards/UiStandardsButtonsPanel";
import { UiStandardsChipsPanel } from "@/features/ui-standards/UiStandardsChipsPanel";
import { UiStandardsTabsPanel } from "@/features/ui-standards/UiStandardsTabsPanel";
import { UiStandardsModuleSubnavPanel } from "@/features/ui-standards/UiStandardsModuleSubnavPanel";
import { UiStandardsActionChipsPanel } from "@/features/ui-standards/UiStandardsActionChipsPanel";
import { UiStandardsCardsPanel } from "@/features/ui-standards/UiStandardsCardsPanel";
import { UiStandardsTablesPanel } from "@/features/ui-standards/UiStandardsTablesPanel";
import { UiStandardsSimpleCatalog } from "@/features/ui-standards/UiStandardsSimpleCatalog";
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
  it("covers every implementable Button sample", () => {
    render(<UiStandardsButtonsPanel {...alwaysOpen} />);
    const counts = assertEveryImplementableSampleHasCopy("ui-standards-buttons-section");

    expect(screen.getByTestId("ui-standards-treatment-primary-gradient")).toHaveAttribute(
      "data-has-copy",
      "true",
    );
    expect(screen.getByTestId("ui-standards-treatment-danger-strong-gradient")).toHaveAttribute(
      "data-has-copy",
      "true",
    );
    expect(screen.getByTestId("ui-standards-btn-add-product")).toHaveAttribute("data-has-copy", "true");
    expect(
      within(screen.getByTestId("ui-standards-btn-add-product")).getByTestId("ui-standards-copy-command"),
    ).toHaveAttribute("data-command", "PRIMARY + SOFT + WITH ICON");
    expect(screen.getByTestId("ui-standards-btn-continue")).toHaveAttribute("data-has-copy", "true");
    expect(
      within(screen.getByTestId("ui-standards-btn-continue")).getByTestId("ui-standards-copy-command"),
    ).toHaveAttribute("data-command", "PRIMARY + SOFT + WITH ICON");
    expect(counts.implementable).toBeGreaterThan(50);
  });

  it("covers every implementable Chip sample", () => {
    render(<UiStandardsChipsPanel {...alwaysOpen} />);
    const counts = assertEveryImplementableSampleHasCopy("ui-standards-chips-section");
    expect(counts.implementable).toBeGreaterThan(40);
  });

  it("covers every implementable Tabs sample including real-world", () => {
    render(<UiStandardsTabsPanel {...alwaysOpen} />);
    const counts = assertEveryImplementableSampleHasCopy("ui-standards-tabs-section");

    expect(screen.getByTestId("ui-standards-tabs-rw-order-status")).toHaveAttribute(
      "data-has-copy",
      "true",
    );
    expect(screen.getByTestId("ui-standards-tabs-rw-catalog-icons")).toHaveAttribute(
      "data-has-copy",
      "true",
    );
    expect(screen.getByTestId("ui-standards-tabs-rw-products-underline")).toHaveAttribute(
      "data-has-copy",
      "true",
    );
    expect(
      within(screen.getByTestId("ui-standards-tabs-rw-order-status")).getByTestId(
        "ui-standards-copy-command",
      ),
    ).toHaveAttribute("data-command", "PILL TABS + WITH COUNT");

    expect(counts.implementable).toBeGreaterThan(30);
  });

  it("covers every implementable Module Subnav sample including real-world", () => {
    render(<UiStandardsModuleSubnavPanel {...alwaysOpen} />);
    const counts = assertEveryImplementableSampleHasCopy("ui-standards-module-subnav-section");

    expect(screen.getByTestId("ui-standards-module-subnav-rw-purchasing")).toHaveAttribute(
      "data-has-copy",
      "true",
    );
    expect(
      within(screen.getByTestId("ui-standards-module-subnav-rw-purchasing")).getByTestId(
        "ui-standards-copy-command",
      ),
    ).toHaveAttribute(
      "data-command",
      "MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE",
    );
    expect(
      within(screen.getByTestId("ui-standards-module-subnav-pillbar-recommended")).getByTestId(
        "ui-standards-copy-command",
      ),
    ).toHaveAttribute(
      "data-command",
      "MODULE SUBNAV + PILL BAR + WITH ICON + WITH COUNT + SOLID PRIMARY ACTIVE",
    );

    expect(counts.implementable).toBeGreaterThan(20);
  });

  it("covers every implementable Action Chip sample including real-world", () => {
    render(
      <MemoryRouter>
        <UiStandardsActionChipsPanel {...alwaysOpen} />
      </MemoryRouter>,
    );
    const counts = assertEveryImplementableSampleHasCopy("ui-standards-action-chips-section");

    expect(screen.getByTestId("ui-standards-action-chip-rw-inventory-responsiveAuto")).toHaveAttribute(
      "data-has-copy",
      "true",
    );
    expect(
      within(screen.getByTestId("ui-standards-action-chip-rw-inventory-responsiveAuto")).getByTestId(
        "ui-standards-copy-command",
      ),
    ).toHaveAttribute("data-command", "ACTION CHIP GROUP + RESPONSIVE AUTO + WITH ICON");
    expect(screen.getByTestId("ui-standards-action-chip-group-wrap")).toHaveAttribute(
      "data-has-copy",
      "true",
    );
    expect(counts.implementable).toBeGreaterThan(30);
  });

  it("covers every implementable Card sample including footers and header actions", () => {
    render(<UiStandardsCardsPanel {...alwaysOpen} />);
    const counts = assertEveryImplementableSampleHasCopy("ui-standards-cards-section");

    const orderSummary = screen.getByText("ORDER SUMMARY").closest("[data-ui-standards-sample]")!;
    expect(orderSummary).toHaveAttribute("data-has-copy", "true");
    expect(within(orderSummary).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "SUMMARY CARD + BORDERED + WITH CHIP",
    );

    for (const label of ["A · ACTION FOOTER", "B · METADATA FOOTER", "C · SPLIT FOOTER"]) {
      const sample = screen.getByText(label).closest("[data-ui-standards-sample]")!;
      expect(sample).toHaveAttribute("data-has-copy", "true");
      expect(within(sample).getByTestId("ui-standards-copy-command")).toHaveAttribute(
        "data-command",
        "BASIC CARD + WITH FOOTER",
      );
    }

    const headerMore = screen.getByText("ICON ONLY · ROUND · GHOST").closest("[data-ui-standards-sample]")!;
    expect(within(headerMore).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "BASIC CARD + WITH ACTIONS",
    );
    const headerRefresh = screen.getByText("REFRESH").closest("[data-ui-standards-sample]")!;
    expect(within(headerRefresh).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "BASIC CARD + WITH ACTIONS",
    );
    const withChips = screen.getByText("ACCOUNT DETAILS").closest("[data-ui-standards-sample]")!;
    expect(within(withChips).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "BASIC CARD + WITH CHIP",
    );

    expect(counts.implementable).toBeGreaterThan(40);
  });

  it("covers every implementable Table sample", () => {
    render(<UiStandardsTablesPanel {...alwaysOpen} />);
    const counts = assertEveryImplementableSampleHasCopy("ui-standards-tables-section");
    expect(screen.getAllByTestId("ui-standards-copy-command").length).toBeGreaterThan(15);
    expect(counts.implementable).toBeGreaterThan(15);
  });

  it("covers every implementable Simple catalog sample", () => {
    render(
      <MemoryRouter>
        <UiStandardsSimpleCatalog />
      </MemoryRouter>,
    );
    const counts = assertEveryImplementableSampleHasCopy("ui-standards-simple-catalog");
    expect(screen.getByTestId("simple-btn-treatment-gradient")).toHaveAttribute("data-has-copy", "true");
    expect(screen.getByTestId("simple-action-icons")).toHaveAttribute("data-has-copy", "true");
    expect(
      within(screen.getByTestId("simple-action-icons")).getByTestId("ui-standards-copy-command"),
    ).toHaveAttribute("data-command", "ACTION CHIP + SOFT + WITH ICON");
    expect(counts.implementable).toBeGreaterThan(40);
  });

  it("formats cross-standard clipboard text with Context prefix", () => {
    const text = formatUiStandardsCursorClipboard(
      ["Card", "Chip"],
      "BASIC CARD + WITH CHIP",
      "B2B = TAG CHIP INFO\nActive = STATUS CHIP SUCCESS",
    );
    expect(text).toContain("Use the locked ExItS Card Standard and ExItS Chip Standard.");
    expect(text).toContain("Apply: BASIC CARD + WITH CHIP.");
    expect(text).toContain("Context:");
    expect(text).toContain("B2B = TAG CHIP INFO");
    expect(text).toContain(
      "Preserve existing business behavior, domain rules, permissions, data flow, and API behavior unless explicitly instructed otherwise.",
    );
  });
});
