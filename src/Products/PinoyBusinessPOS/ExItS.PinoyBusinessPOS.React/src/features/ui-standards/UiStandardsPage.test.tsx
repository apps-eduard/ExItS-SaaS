import { beforeEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { catalogs } from "@/i18n/messages";
import { ShellUiStandardsButton } from "@/components/exits/ShellUiStandardsButton";
import { UiStandardsPage } from "@/features/ui-standards/UiStandardsPage";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
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
    <PreferencesProvider>
      <ToastProvider>
        <MemoryRouter initialEntries={[entry]}>
          <Routes>
            <Route path="/ui-standards" element={<UiStandardsPage />} />
          </Routes>
        </MemoryRouter>
      </ToastProvider>
    </PreferencesProvider>,
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
    expect(screen.getByTestId("ui-standards-preference-preview")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-button-playground")).toBeInTheDocument();
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

  it("renders Prime-style Button gallery sections mapped to ExItS axes", () => {
    renderPage();
    const gallery = screen.getByTestId("ui-standard-button-gallery");
    expect(gallery).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-default")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-severities")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-text")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-outlined")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-group")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-splitbutton")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-templating")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-icons")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-raised")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-rounded")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-rounded-icons")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-rounded-text")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-rounded-outlined")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-loading")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-severity-primary")).toHaveAttribute(
      "data-gallery-severity",
      "primary",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-severity-help")).toHaveAttribute(
      "data-gallery-severity",
      "help",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-severity-warn")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-severity-secondary")).toHaveAttribute(
      "data-gallery-severity",
      "secondary",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-severity-contrast")).toHaveAttribute(
      "data-gallery-severity",
      "contrast",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-severity-primary").className).toContain(
      "exits-severity-btn",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-severity-primary").className).toContain(
      "var(--exits-severity-primary)",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-severity-secondary").className).toContain(
      "var(--exits-severity-secondary)",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-severities").textContent).not.toMatch(
      /Primary · Secondary · Success/,
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-group-save")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-split-primary")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-template-primeng")).toHaveTextContent(
      "PrimeNG",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-round-text-success")).toHaveAttribute(
      "data-appearance",
      "ghost",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-round-outlined-danger")).toHaveAttribute(
      "data-appearance",
      "outline",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-loading-plain")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-gallery-raised-success")).toHaveAttribute(
      "data-appearance",
      "elevated",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-text-danger")).toHaveAttribute(
      "data-appearance",
      "ghost",
    );
    expect(screen.getByTestId("ui-standard-btn-gallery-outlined-info")).toHaveAttribute(
      "data-appearance",
      "outline",
    );
  });

  it("updates Button playground preview and snippet from selected props", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.selectOptions(screen.getByTestId("ui-standard-btn-pg-intent"), "danger");
    await user.selectOptions(screen.getByTestId("ui-standard-btn-pg-appearance"), "outline");
    const previewBtn = within(screen.getByTestId("ui-standard-btn-pg-preview")).getByRole("button");
    expect(previewBtn).toHaveAttribute("data-intent", "danger");
    expect(previewBtn).toHaveAttribute("data-appearance", "outline");
    expect(previewBtn).toHaveTextContent("Save");
    const snippet = screen.getByTestId("ui-standard-btn-pg-snippet");
    expect(snippet).toHaveTextContent('intent="danger"');
    expect(snippet).toHaveTextContent('appearance="outline"');
    expect(snippet).toHaveTextContent('shape="auto"');
  });

  it("copies Button playground snippet via clipboard and toast", async () => {
    const user = userEvent.setup();
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.defineProperty(navigator, "clipboard", {
      configurable: true,
      value: { writeText },
    });
    renderPage();
    await user.click(screen.getByTestId("ui-standard-btn-pg-copy"));
    expect(writeText).toHaveBeenCalled();
    expect(writeText.mock.calls[0][0]).toContain("<Button");
    expect(writeText.mock.calls[0][0]).toContain('intent="primary"');
    expect(await screen.findByText("Snippet copied")).toBeInTheDocument();
  });

  it("updates StatusChip playground and keeps fixed samples below", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.selectOptions(screen.getByTestId("ui-standard-status-pg-tone"), "warning");
    await user.selectOptions(screen.getByTestId("ui-standard-status-pg-appearance"), "solid");
    await user.selectOptions(screen.getByTestId("ui-standard-status-pg-shape"), "pill");
    const chip = within(screen.getByTestId("ui-standard-status-pg-preview")).getByText("Active");
    expect(chip).toHaveAttribute("data-tone", "warning");
    expect(chip).toHaveAttribute("data-appearance", "solid");
    expect(chip).toHaveAttribute("data-shape", "pill");
    expect(screen.getByTestId("ui-standard-status-pg-snippet")).toHaveTextContent('tone="warning"');
    expect(screen.getByTestId("ui-standard-status-shape-auto-soft")).toBeInTheDocument();
  });

  it("renders ExitsSelect playground with real API only", () => {
    renderPage();
    expect(screen.getByTestId("ui-standard-select-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-select-pg-control")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-select-pg-snippet")).toHaveTextContent("<ExitsSelect");
    expect(screen.getByTestId("ui-standard-select-pg-snippet")).toHaveTextContent("onChange={setValue}");
    expect(screen.queryByTestId("ui-standard-select-pg-shape")).not.toBeInTheDocument();
  });

  it("wires Global Preference Preview to the real preference store", async () => {
    const user = userEvent.setup();
    renderPage();
    const preview = screen.getByTestId("ui-standards-preference-preview");
    expect(within(preview).getByText("Global Preference Preview")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-preference-auto-samples")).toBeInTheDocument();
    await user.click(within(screen.getByTestId("ui-standards-pref-control-shape")).getByRole("radio", {
      name: "Control Shape: Pill",
    }));
    expect(document.documentElement.dataset.controlShape).toBe("pill");
    expect(
      within(screen.getByTestId("ui-standards-preference-auto-samples")).getByRole("button", {
        name: "Save",
      }),
    ).toBeInTheDocument();
    expect(
      within(screen.getByTestId("ui-standards-preference-auto-samples")).getByText("Active"),
    ).toHaveAttribute("data-shape", "auto");
    expect(screen.getByTestId("ui-standards-preference-search")).toHaveAttribute(
      "data-shape",
      "auto",
    );
    const qty = screen.getByTestId("ui-standards-preference-qty").closest(
      "[data-testid='quantity-stepper']",
    );
    expect(qty?.className).toContain("quantity-stepper--outline");
    expect(qty?.className).toContain("quantity-stepper--cart");
    expect(within(preview).getByText("QuantityStepper Outline (standard)")).toBeInTheDocument();
  });

  it("shows Do / Don’t guidance card", () => {
    renderPage();
    const card = screen.getByTestId("ui-standard-card-dodont");
    expect(within(card).getByText("Do / Don’t")).toBeInTheDocument();
    expect(within(card).getByText("Use one Primary action per action group.")).toBeInTheDocument();
    expect(
      within(card).getByText("Intent describes meaning. Appearance describes presentation."),
    ).toBeInTheDocument();
    expect(
      within(card).getByText("Soft appearance and Soft shape are independent properties."),
    ).toBeInTheDocument();
    expect(within(card).getByText(/Desktop TABLE/)).toBeInTheDocument();
  });

  it("finds playground and Do/Don’t via search keywords", async () => {
    const user = userEvent.setup();
    renderPage();
    const search = screen.getByTestId("ui-standards-search").querySelector("input");
    expect(search).toBeTruthy();

    await user.clear(search!);
    await user.type(search!, "playground");
    expect(screen.getByTestId("ui-standard-card-buttons")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-status")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-selects")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-dodont")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-preference-preview")).toBeInTheDocument();

    await user.clear(search!);
    await user.type(search!, "primary");
    expect(screen.getByTestId("ui-standard-card-buttons")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-dodont")).toBeInTheDocument();

    await user.clear(search!);
    await user.type(search!, "auto");
    expect(screen.getByTestId("ui-standard-button-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-status-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-preference-preview")).toBeInTheDocument();
  });

  it("shows Soft Outline Solid appearances on the Status live card", () => {
    renderPage();
    const status = screen.getByTestId("ui-standard-card-status");
    expect(within(status).getAllByText("Tone").length).toBeGreaterThanOrEqual(1);
    expect(within(status).getAllByText("Appearance").length).toBeGreaterThanOrEqual(1);
    expect(within(status).getAllByText("Soft").length).toBeGreaterThanOrEqual(2);
    expect(within(status).getAllByText("Outline").length).toBeGreaterThanOrEqual(2);
    expect(within(status).getAllByText("Solid").length).toBeGreaterThanOrEqual(2);
    expect(status.querySelectorAll('[data-appearance="solid"]').length).toBeGreaterThan(0);
    expect(status.querySelectorAll('[data-appearance="outline"]').length).toBeGreaterThan(0);
  });

  it("shows Auto and explicit Control Shape samples on the Status live card", () => {
    renderPage();
    const status = screen.getByTestId("ui-standard-card-status");
    expect(within(status).getAllByText("Shape").length).toBeGreaterThanOrEqual(1);
    expect(within(status).getByTestId("ui-standard-status-shape-auto-soft")).toHaveAttribute(
      "data-shape",
      "auto",
    );
    expect(within(status).getByTestId("ui-standard-status-shape-standard-outline")).toHaveAttribute(
      "data-shape",
      "standard",
    );
    expect(within(status).getByTestId("ui-standard-status-shape-soft-solid")).toHaveAttribute(
      "data-shape",
      "soft",
    );
    expect(within(status).getByTestId("ui-standard-status-shape-pill-soft")).toHaveAttribute(
      "data-shape",
      "pill",
    );
    expect(within(status).getByTestId("ui-standard-status-shape-grid").className).toMatch(
      /repeat\(3/,
    );
    expect(
      within(status).getByText(/Auto follows the global Control Shape preference/, {
        exact: false,
      }),
    ).toBeInTheDocument();
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
    expect(screen.getByTestId("ui-standard-card-table")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-responsive-data-preview-options")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-responsive-data-category-filter")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-rdv-head-category")).toBeInTheDocument();
    expect(within(screen.getByTestId("ui-standard-card-table")).getAllByText("Retail").length).toBeGreaterThanOrEqual(1);
    expect(screen.getByTestId("ui-standard-responsive-data-search")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-rdv-head-customer-sort")).toBeInTheDocument();
    expect(
      within(screen.getByTestId("ui-standard-responsive-data-pagination")).getByTestId(
        "exits-table-page-size",
      ),
    ).toBeInTheDocument();

    expect(screen.getByTestId("ui-standards-table")).toHaveClass("exits-table-container");
    expect(screen.getByTestId("ui-standards-table-demo")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-table-search")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-responsive-data")).toBeInTheDocument();
    expect(
      within(screen.getByTestId("ui-standard-card-table")).getByText("Responsive Data View"),
    ).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-table-mode")).toHaveTextContent(
      "Desktop · 1280px · TABLE",
    );
    expect(screen.getByTestId("ui-standards-table-preview-layout")).toHaveAttribute(
      "data-layout",
      "table",
    );
    expect(screen.getByTestId("ui-standards-table-frame")).toHaveAttribute(
      "data-preview-width",
      "1280",
    );
    expect(screen.getByTestId("ui-standard-responsive-data-mode")).toHaveTextContent(
      "Desktop · 1280px · TABLE",
    );
    expect(screen.getByTestId("ui-standard-responsive-data")).toHaveAttribute(
      "data-layout",
      "table",
    );
    expect(screen.getByTestId("ui-standard-responsive-data-frame")).toHaveAttribute(
      "data-preview-width",
      "1280",
    );
    expect(screen.getAllByText("Apple").length).toBeGreaterThanOrEqual(1);

    const appleRow = screen.getByTestId("ui-standards-main-row-apple");
    await user.click(within(appleRow).getByRole("button", { name: "Edit Apple" }));
    expect(screen.getByRole("menuitem", { name: "SKU" })).toBeInTheDocument();
  });

  it("Responsive Data View locks search, sort, export, page size, and category filter as default", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-data"));

    const card = screen.getByTestId("ui-standard-card-table");
    expect(within(card).getByTestId("ui-standard-responsive-data-search")).toBeInTheDocument();
    expect(within(card).getByTestId("ui-standard-responsive-data-category-filter")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-rdv-head-customer-sort")).toBeInTheDocument();
    expect(
      within(screen.getByTestId("ui-standard-responsive-data-pagination")).getByTestId(
        "exits-table-page-size",
      ),
    ).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-responsive-data-preview-options")).not.toBeInTheDocument();
  });

  it("switches Responsive Data View device preview between TABLE and LIST", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-data"));

    const card = screen.getByTestId("ui-standard-card-table");
    expect(within(card).getAllByText("Juan Dela Cruz").length).toBeGreaterThanOrEqual(1);
    expect(screen.getByTestId("ui-standard-responsive-data")).toHaveAttribute(
      "data-layout",
      "table",
    );

    await user.click(
      within(screen.getByTestId("ui-standard-responsive-data-preview-size")).getByRole("radio", {
        name: "Preview size: Tablet",
      }),
    );
    expect(screen.getByTestId("ui-standard-responsive-data-mode")).toHaveTextContent(
      "Tablet · 834px · LIST",
    );
    expect(screen.getByTestId("ui-standard-responsive-data")).toHaveAttribute(
      "data-layout",
      "list",
    );
    expect(screen.getByTestId("ui-standard-responsive-data-frame")).toHaveAttribute(
      "data-preview-width",
      "834",
    );
    const records = screen.getByTestId("ui-standard-responsive-data-records");
    expect(within(records).getByText("Paul Coffee")).toBeInTheDocument();
    expect(within(records).getByText("Ana Santos")).toBeInTheDocument();
    expect(within(records).getByText("Juan Dela Cruz")).toBeInTheDocument();

    await user.click(
      within(screen.getByTestId("ui-standard-responsive-data-preview-size")).getByRole("radio", {
        name: "Preview size: Mobile",
      }),
    );
    expect(screen.getByTestId("ui-standard-responsive-data-mode")).toHaveTextContent(
      "Mobile · 390px · LIST",
    );
    expect(screen.getByTestId("ui-standard-responsive-data")).toHaveAttribute(
      "data-layout",
      "list",
    );
    expect(screen.getByTestId("ui-standard-responsive-data-frame")).toHaveAttribute(
      "data-preview-width",
      "390",
    );
    expect(within(card).getByTestId("ui-standard-list-edit-1")).toBeInTheDocument();
    expect(within(card).queryByTestId("ui-standard-table-search")).not.toBeInTheDocument();
    expect(within(records).getByText("Ana Santos")).toBeInTheDocument();
    expect(within(records).getByText("Juan Dela Cruz")).toBeInTheDocument();
    expect(within(card).getByTestId("ui-standard-responsive-data-pagination")).toBeInTheDocument();
    expect(
      within(card).getByTestId("ui-standard-responsive-data-pagination").querySelector(
        "[data-testid='exits-table-page']",
      ),
    ).toHaveTextContent("1 / 2");
  });

  it("switches Approved ExitsTable device preview (md 768: Desktop/Tablet TABLE, Mobile LIST)", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-data"));

    expect(screen.getByTestId("ui-standards-table-mode")).toHaveTextContent(
      "Desktop · 1280px · TABLE",
    );
    expect(screen.getByTestId("ui-standards-table-preview-layout")).toHaveAttribute(
      "data-layout",
      "table",
    );
    expect(screen.getByTestId("ui-standards-main-row-apple")).toBeInTheDocument();

    await user.click(
      within(screen.getByTestId("ui-standards-table-preview-size")).getByRole("radio", {
        name: "Preview size: Tablet",
      }),
    );
    expect(screen.getByTestId("ui-standards-table-mode")).toHaveTextContent(
      "Tablet · 834px · TABLE",
    );
    expect(screen.getByTestId("ui-standards-table-preview-layout")).toHaveAttribute(
      "data-layout",
      "table",
    );
    expect(screen.getByTestId("ui-standards-table-frame")).toHaveAttribute(
      "data-preview-width",
      "834",
    );

    await user.click(
      within(screen.getByTestId("ui-standards-table-preview-size")).getByRole("radio", {
        name: "Preview size: Mobile",
      }),
    );
    expect(screen.getByTestId("ui-standards-table-mode")).toHaveTextContent(
      "Mobile · 390px · LIST",
    );
    expect(screen.getByTestId("ui-standards-table-preview-layout")).toHaveAttribute(
      "data-layout",
      "list",
    );
    expect(screen.getByTestId("ui-standards-mobile-row-apple")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-mobile-select-all")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-mobile-select-all-label")).toHaveTextContent(
      /Select all \(10\)/i,
    );

    await user.click(screen.getByTestId("ui-standards-mobile-select-all-checkbox"));
    expect(screen.getByTestId("ui-standards-selected-count")).toHaveTextContent("10 selected");
    expect(screen.getByTestId("ui-standards-mobile-select-all-label")).toHaveTextContent(
      /Deselect all/i,
    );
    expect(screen.getByTestId("ui-standards-mobile-row-apple")).toHaveAttribute(
      "data-selected",
      "true",
    );

    await user.click(screen.getByTestId("ui-standards-view-apple"));
    expect(screen.getByTestId("ui-standards-table-view-modal")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-table-view-modal")).toHaveTextContent("Apple");
    expect(screen.getByTestId("ui-standards-table-view-modal")).toHaveTextContent("PH-FRU-APPLE");
    await user.click(screen.getByTestId("ui-standards-table-view-modal-dismiss"));
    expect(screen.queryByTestId("ui-standards-table-view-modal")).not.toBeInTheDocument();
  });

  it("finds responsive data preview via desktop/tablet/mobile search", async () => {
    const user = userEvent.setup();
    renderPage();
    const search = screen.getByTestId("ui-standards-search").querySelector("input");
    expect(search).toBeTruthy();
    await user.clear(search!);
    await user.type(search!, "mobile");
    expect(screen.getByTestId("ui-standard-card-table")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-responsive-data-preview")).toBeInTheDocument();
  });

  it("ignores obsolete ?view= classic/simple params", () => {
    renderPage("/ui-standards?view=simple");
    expect(screen.queryByTestId("ui-standards-simple-catalog")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-live-section")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-view-switcher")).not.toBeInTheDocument();
  });

  it("opens Forms filter with Form Controls and QuantityStepper variant card", () => {
    renderPage("/ui-standards?category=forms");
    expect(screen.getByTestId("ui-standards-filter-forms")).toHaveAttribute(
      "data-selected",
      "true",
    );
    expect(screen.getByTestId("ui-standard-card-forms")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-quantity-stepper")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-quantity-stepper")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-qty-outline").closest("[data-testid='quantity-stepper']")?.className).toContain(
      "quantity-stepper--outline",
    );
    expect(screen.getByTestId("ui-standard-qty-auto").closest("[data-testid='quantity-stepper']")?.className).toContain(
      "quantity-stepper--auto",
    );
    expect(screen.getByTestId("ui-standard-qty-field").closest("[data-testid='quantity-stepper']")?.className).toContain(
      "quantity-stepper--field",
    );
    expect(screen.getByTestId("ui-standard-qty-standard")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-qty-soft")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-qty-pill")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-quantity-stepper-legacy")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-qty-legacy")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-catalog-row-quantity-stepper")).toBeInTheDocument();
  });

  it("opens Buttons filter from category=buttons with full button gallery", () => {
    renderPage("/ui-standards?category=buttons");
    expect(screen.getByTestId("ui-standards-filter-buttons")).toHaveAttribute(
      "data-selected",
      "true",
    );
    expect(screen.getByTestId("ui-standard-card-buttons")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-button-gallery")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-dodont")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-upload")).not.toBeInTheDocument();
  });

  it("opens Messages filter from category=messages with full Diamond message gallery", () => {
    renderPage("/ui-standards?category=messages");
    expect(screen.getByTestId("ui-standards-filter-messages")).toHaveAttribute(
      "data-selected",
      "true",
    );
    expect(screen.getByTestId("ui-standard-card-messages")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-message-gallery")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-gallery-toast")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-gallery-severity")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-gallery-custom")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-gallery-expanded")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-gallery-action")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-gallery-inline")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-gallery-message")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-gallery-outlined")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-gallery-simple")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-msg-filled-success")).toHaveAttribute(
      "data-gallery-message-severity",
      "success",
    );
    expect(screen.getByTestId("ui-standard-msg-filled-success").className).toContain("outline-[#bbf7d0]");
    expect(screen.getByTestId("ui-standard-msg-filled-success").className).toContain("border-0");
    expect(screen.getByTestId("ui-standard-msg-outlined-error")).toHaveAttribute(
      "data-gallery-message-variant",
      "outlined",
    );
    expect(screen.getByTestId("ui-standard-msg-outlined-error").className).toContain("outline-[#dc2626]");
    expect(screen.getByTestId("ui-standard-msg-outlined-error").className).toContain("bg-transparent");
    expect(screen.getByTestId("ui-standard-msg-simple-contrast")).toHaveAttribute(
      "data-gallery-message-variant",
      "simple",
    );
    expect(screen.getByTestId("ui-standard-msg-inline-banner")).toHaveTextContent(
      "Validation Failed",
    );
    expect(screen.queryByTestId("ui-standard-card-buttons")).not.toBeInTheDocument();
  });

  it("maps legacy category=actions to Buttons filter", () => {
    renderPage("/ui-standards?category=actions");
    expect(screen.getByTestId("ui-standards-filter-buttons")).toHaveAttribute(
      "data-selected",
      "true",
    );
    expect(screen.getByTestId("ui-standard-card-buttons")).toBeInTheDocument();
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
    expect(link).toHaveAttribute("href", "/ui-standards?category=buttons");
    expect(link.textContent?.replace(/\s+/g, " ").trim()).toBe("UI Standards");
    expect(link.querySelector("svg")).not.toBeNull();
  });
});
