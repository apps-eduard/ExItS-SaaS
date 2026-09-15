import { beforeEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { catalogs } from "@/i18n/messages";
import { ShellUiStandardsButton } from "@/components/exits/ShellUiStandardsButton";
import { UiStandardsPage } from "@/features/ui-standards/UiStandardsPage";
import { ToastProvider } from "@/components/exits/ToastProvider";
import {
  UI_STANDARDS_DEFAULT_OPEN,
  UI_STANDARDS_SECTIONS_STORAGE_KEY,
  createDefaultUiStandardsDisclosure,
  readUiStandardsDisclosure,
  writeUiStandardsDisclosure,
} from "@/features/ui-standards/ui-standards-disclosure";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: keyof typeof catalogs.en) => catalogs.en[key] ?? String(key),
  }),
}));

vi.mock("@/api/platform/local-validation-gate", () => ({
  isFrontendLocalValidationMode: () => true,
}));

function renderPage(entry = "/ui-standards") {
  return render(
    <ToastProvider>
      <MemoryRouter initialEntries={[entry]}>
        <Routes>
          <Route path="/ui-standards" element={<UiStandardsPage />} />
        </Routes>
      </MemoryRouter>
    </ToastProvider>,
  );
}

describe("ui-standards disclosure storage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("uses ExItS-namespaced key and restores known booleans only", () => {
    expect(UI_STANDARDS_SECTIONS_STORAGE_KEY).toBe("exits.uiStandards.sections.v1");
    writeUiStandardsDisclosure({
      ...createDefaultUiStandardsDisclosure(),
      "tables.demo": true,
      "unknown.key": true as unknown as boolean,
    });
    const restored = readUiStandardsDisclosure();
    expect(restored["tables.demo"]).toBe(true);
    expect(restored["tables.alignment"]).toBe(UI_STANDARDS_DEFAULT_OPEN["tables.alignment"]);
    expect(Object.keys(restored)).not.toContain("unknown.key");
  });
});

describe("UiStandardsPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("renders the canonical consolidated page without Classic/Simple toggle", () => {
    renderPage();

    expect(screen.getByTestId("ui-standards-page")).toBeInTheDocument();
    expect(screen.getByText("ExItS UI Standard")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-filter-bar")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-live-section")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-cards")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-buttons")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-intent-primary")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-appearance-solid")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-view-switcher")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-view-classic")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-view-simple")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-simple-catalog")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-sticky-nav")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-buttons-section")).not.toBeInTheDocument();
  });

  it("keeps Intent separate from Appearance on the Buttons live card", () => {
    renderPage();
    const buttons = screen.getByTestId("ui-standard-card-buttons");
    expect(within(buttons).getByText("Intent / Tone")).toBeInTheDocument();
    expect(within(buttons).getByText("Appearance / Treatment")).toBeInTheDocument();
    expect(within(buttons).queryByTestId("ui-standard-intent-outline")).not.toBeInTheDocument();
    expect(within(buttons).queryByTestId("ui-standard-intent-ghost")).not.toBeInTheDocument();
    expect(within(buttons).getByTestId("ui-standard-appearance-outline")).toBeInTheDocument();
    expect(within(buttons).getByTestId("ui-standard-appearance-ghost")).toBeInTheDocument();
    expect(within(buttons).getByTestId("ui-standard-appearance-elevated")).toBeInTheDocument();
  });

  it("shows Soft Outline Solid appearances on the Status live card", () => {
    renderPage();
    const status = screen.getByTestId("ui-standard-card-status");
    expect(within(status).getByText("Tone")).toBeInTheDocument();
    expect(within(status).getByText("Appearance")).toBeInTheDocument();
    expect(within(status).getByText("Soft")).toBeInTheDocument();
    expect(within(status).getByText("Outline")).toBeInTheDocument();
    expect(within(status).getByText("Solid")).toBeInTheDocument();
    expect(status.querySelectorAll('[data-appearance="solid"]').length).toBeGreaterThan(0);
    expect(status.querySelectorAll('[data-appearance="outline"]').length).toBeGreaterThan(0);
  });

  it("filters live cards and catalog rows by category + search", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByTestId("ui-standards-filter-overlays"));
    expect(screen.getByTestId("ui-standard-card-drawer")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-modal")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-confirm")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-buttons")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-catalog-row-form-drawer")).toBeInTheDocument();

    await user.click(screen.getByTestId("ui-standards-filter-selects"));
    expect(screen.getByTestId("ui-standard-card-selects")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-select-standard")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-select-multi")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-select-switch")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-forms")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-catalog-row-exits-select")).toBeInTheDocument();

    await user.click(screen.getByTestId("ui-standards-filter-all"));
    const search = screen.getByTestId("ui-standards-search").querySelector("input");
    expect(search).toBeTruthy();
    await user.clear(search!);
    await user.type(search!, "toast");
    expect(screen.getByTestId("ui-standard-card-toasts")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-table")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-catalog-row-toast")).toBeInTheDocument();
  });

  it("Data filter surfaces the locked ExitsTable reference demo", async () => {
    const user = userEvent.setup();
    renderPage();

    await user.click(screen.getByTestId("ui-standards-filter-data"));
    expect(screen.getByTestId("ui-standards-data-exits-table")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-tables-section")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-table")).toHaveClass("exits-table-container");
    expect(screen.getByTestId("ui-standards-table-demo")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-table")).toBeInTheDocument();
    expect(screen.getAllByText("Apple").length).toBeGreaterThanOrEqual(1);

    const appleRow = screen.getByTestId("ui-standards-main-row-apple");
    await user.click(within(appleRow).getByRole("button", { name: "Edit Apple" }));
    expect(screen.getByRole("menuitem", { name: "SKU" })).toBeInTheDocument();
  });

  it("ignores obsolete ?view= classic/simple params", () => {
    renderPage("/ui-standards?view=simple");
    expect(screen.queryByTestId("ui-standards-simple-catalog")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-live-section")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-view-switcher")).not.toBeInTheDocument();
  });
});

describe("ShellUiStandardsButton", () => {
  it("renders an icon-only Palette control with UI Standards a11y labels", () => {
    render(
      <MemoryRouter>
        <ShellUiStandardsButton label="UI Standards" />
      </MemoryRouter>,
    );

    const link = screen.getByTestId("shell-ui-standards-button");
    expect(link).toHaveAttribute("aria-label", "UI Standards");
    expect(link).toHaveAttribute("title", "UI Standards");
    expect(link).toHaveAttribute("href", "/ui-standards");
    expect(link.textContent?.replace(/\s+/g, " ").trim()).toBe("UI Standards");
    expect(link.querySelector("svg")).not.toBeNull();
  });
});
