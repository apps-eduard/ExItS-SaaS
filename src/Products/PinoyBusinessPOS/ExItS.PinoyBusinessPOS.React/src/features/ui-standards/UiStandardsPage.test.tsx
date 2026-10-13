import { beforeEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { cleanup, render, screen, within } from "@testing-library/react";
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
    expect(screen.queryByTestId("ui-standards-search")).not.toBeInTheDocument();
    const samples = screen.getByTestId("ui-standard-cards");
    const playgroundCard = within(samples).getByTestId("ui-standard-card-button-playground");
    const buttonsCard = within(samples).getByTestId("ui-standard-card-buttons");
    expect(playgroundCard.compareDocumentPosition(buttonsCard) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(screen.getByTestId("ui-standards-live-section")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-cards")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-buttons")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-preference-preview")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-button-playground")).toBeInTheDocument();
    expect(
      within(screen.getByTestId("ui-standard-card-button-playground")).getByText(
        "Button playground",
      ),
    ).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-button-gallery")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-view-switcher")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-view-classic")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-view-simple")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-simple-catalog")).not.toBeInTheDocument();
    const sidenav = screen.getByTestId("ui-standards-sidenav");
    expect(sidenav).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-category-filters")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-filter-all")).toHaveTextContent("Global preview");
    expect(screen.getByTestId("ui-standards-filter-all")).toHaveAttribute("data-selected", "true");
    expect(within(screen.getByTestId("ui-standards-main")).getByTestId("ui-standards-preference-preview")).toBeInTheDocument();
    expect(within(sidenav).queryByTestId("ui-standards-preference-preview")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-sticky-nav")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-buttons-section")).not.toBeInTheDocument();
  });

  it("keeps the Buttons live card on the Quasar samples", () => {
    renderPage();
    const buttons = screen.getByTestId("ui-standard-card-buttons");
    expect(within(buttons).getByTestId("ui-standard-button-gallery")).toBeInTheDocument();
    expect(within(buttons).queryByText("Common actions")).not.toBeInTheDocument();
    expect(within(buttons).queryByText("One Primary per group")).not.toBeInTheDocument();
    expect(within(buttons).queryByText("Intent / Tone (8 locked)")).not.toBeInTheDocument();
    expect(within(buttons).queryByText("Appearance / Treatment")).not.toBeInTheDocument();
    expect(within(buttons).queryByText("States")).not.toBeInTheDocument();
    expect(within(buttons).queryByTestId("ui-standard-intent-primary")).not.toBeInTheDocument();
    expect(within(buttons).queryByTestId("ui-standard-appearance-solid")).not.toBeInTheDocument();
  });

  it("renders Prime-style Button gallery sections mapped to ExItS axes", () => {
    renderPage();
    const gallery = screen.getByTestId("ui-standard-button-gallery");
    expect(gallery).toBeInTheDocument();
    const quasar = screen.getByTestId("ui-standard-btn-quasar-standard");
    expect(quasar).toBeInTheDocument();
    expect(within(quasar).getByText("Standard buttons")).toBeInTheDocument();
    expect(within(quasar).getByText("cursor: ext-btn-standard-primary")).toBeInTheDocument();
    const outline = screen.getByTestId("ui-standard-btn-quasar-outline");
    expect(within(outline).getByText("Button outline")).toBeInTheDocument();
    expect(within(outline).getByRole("button", { name: "Primary" })).toHaveClass(
      "ui-standards-qbtn--outline",
    );
    expect(within(outline).getByRole("button", { name: "Standard" })).toHaveClass(
      "ui-standards-qbtn--dark-ink",
    );
    expect(within(outline).getByRole("button", { name: "Black" })).toHaveClass(
      "ui-standards-qbtn--dark-ink",
    );
    expect(within(outline).getByRole("button", { name: "Success" })).toBeInTheDocument();
    expect(within(outline).getByRole("button", { name: "Info" })).toBeInTheDocument();
    const raised = screen.getByTestId("ui-standard-btn-quasar-raised");
    expect(within(raised).getByText("Raised")).toBeInTheDocument();
    expect(within(raised).getByRole("button", { name: "Primary" })).toHaveClass(
      "ui-standards-qbtn--raised",
    );
    const roundSolid = screen.getByTestId("ui-standard-btn-quasar-round-solid");
    expect(within(roundSolid).getByText("Rounded icon solid")).toBeInTheDocument();
    expect(within(roundSolid).getByRole("button", { name: "Primary" })).toHaveClass(
      "ui-standards-qbtn--icon",
    );
    const roundOutline = screen.getByTestId("ui-standard-btn-quasar-round-outline");
    expect(within(roundOutline).getByText("Rounded icon outlined")).toBeInTheDocument();
    expect(within(roundOutline).getByRole("button", { name: "Help" })).toHaveClass(
      "ui-standards-qbtn--outline",
    );
    const ghost = screen.getByTestId("ui-standard-btn-quasar-ghost");
    expect(within(ghost).getByText("Button ghost")).toBeInTheDocument();
    expect(within(ghost).getByRole("button", { name: "Primary" })).toHaveClass(
      "ui-standards-qbtn--ghost",
    );
    expect(within(ghost).getByRole("button", { name: "Standard" })).toHaveClass(
      "ui-standards-qbtn--dark-ink",
    );
    expect(within(ghost).getByRole("button", { name: "Black" })).toHaveClass(
      "ui-standards-qbtn--dark-ink",
    );
    expect(within(roundOutline).getByRole("button", { name: "Standard" })).not.toHaveClass(
      "ui-standards-qbtn--dark-ink",
    );
    const glossy = screen.getByTestId("ui-standard-btn-quasar-glossy");
    expect(within(glossy).getByText("Button glossy")).toBeInTheDocument();
    expect(within(glossy).getByRole("button", { name: "Warn" })).toHaveClass(
      "ui-standards-qbtn--glossy",
    );
    expect(
      screen.getByTestId("ui-standard-btn-quasar-raised").compareDocumentPosition(ghost),
    ).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
    expect(ghost.compareDocumentPosition(glossy)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
    const gradient = screen.getByTestId("ui-standard-btn-quasar-gradient");
    expect(within(gradient).getByText("Button gradient")).toBeInTheDocument();
    expect(within(gradient).getByRole("button", { name: "Primary" })).toHaveClass(
      "ui-standards-qbtn--gradient",
    );
    expect(within(gradient).getByRole("button", { name: "Primary" })).toHaveStyle({
      backgroundColor: "#1976d2",
    });
    expect(within(gradient).getByRole("button", { name: "Secondary" })).toHaveStyle({
      backgroundColor: "#26a69a",
    });
    expect(glossy.compareDocumentPosition(gradient)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
    const roundGhost = screen.getByTestId("ui-standard-btn-quasar-round-ghost");
    expect(within(roundGhost).getByText("Rounded icon ghost")).toBeInTheDocument();
    expect(within(roundGhost).getByRole("button", { name: "Primary" })).toHaveClass(
      "ui-standards-qbtn--icon",
      "ui-standards-qbtn--ghost",
    );
    expect(within(roundGhost).getByRole("button", { name: "Standard" })).toHaveClass(
      "ui-standards-qbtn--dark-ink",
    );
    expect(screen.queryByTestId("ui-standard-btn-quasar-rounded-ghost")).not.toBeInTheDocument();
    expect(within(screen.getByTestId("ui-standard-btn-quasar-rounded")).getByRole("button", {
      name: "Primary",
    })).toHaveClass("ui-standards-qbtn--rounded");
    expect(within(screen.getByTestId("ui-standard-btn-quasar-flat")).getByRole("button", {
      name: "Primary",
    })).toHaveClass("ui-standards-qbtn--flat");
    expect(within(screen.getByTestId("ui-standard-btn-quasar-round-icon")).getByText("Rounded icon")).toBeInTheDocument();
    expect(within(screen.getByTestId("ui-standard-btn-quasar-square-icon")).getByRole("button", {
      name: "Primary",
    })).toHaveClass("ui-standards-qbtn--square");
    expect(within(quasar).getByRole("button", { name: "Standard" })).toBeInTheDocument();
    expect(within(quasar).getByRole("button", { name: "Secondary" })).toBeInTheDocument();
    expect(within(quasar).getByRole("button", { name: "Success" })).toBeInTheDocument();
    expect(within(quasar).getByRole("button", { name: "Info" })).toBeInTheDocument();
    expect(within(quasar).getByRole("button", { name: "Amber" })).toBeInTheDocument();
    expect(within(quasar).queryByRole("button", { name: "Brown 5" })).not.toBeInTheDocument();
    expect(within(quasar).getByRole("button", { name: "Danger" })).toBeInTheDocument();
    expect(within(quasar).queryByRole("button", { name: "Deep Orange" })).not.toBeInTheDocument();
    expect(within(quasar).getByRole("button", { name: "Warn" })).toBeInTheDocument();
    expect(within(quasar).queryByRole("button", { name: "Purple" })).not.toBeInTheDocument();
    expect(within(quasar).getByRole("button", { name: "Help" })).toBeInTheDocument();
    expect(within(quasar).getByRole("button", { name: "Black" })).toBeInTheDocument();
    expect(within(screen.getByTestId("ui-standard-card-buttons")).queryByText("Gallery — locked standard")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-btn-gallery-default")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-btn-gallery-severities")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-btn-gallery-loading")).not.toBeInTheDocument();
  });

  it("updates Button playground preview from selected props", async () => {
    const user = userEvent.setup();
    renderPage();
    const playground = screen.getByTestId("ui-standard-card-button-playground");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn",
    );
    const intentSelect = screen.getByTestId("ui-standard-btn-pg-intent");
    const intentOptions = within(intentSelect).getAllByRole("option").map((option) => option.textContent);
    expect(intentOptions[0]).toBe("Standard");
    expect(intentOptions.at(-1)).toBe("Black");
    expect(intentOptions).toHaveLength(10);
    const appearanceSelect = screen.getByTestId("ui-standard-btn-pg-appearance");
    const appearanceOptions = within(appearanceSelect)
      .getAllByRole("option")
      .map((option) => option.textContent);
    expect(appearanceOptions[0]).toBe("Standard");
    expect(appearanceOptions.at(-1)).toBe("Flat");
    await user.selectOptions(intentSelect, "danger");
    await user.selectOptions(appearanceSelect, "outline");
    const previewBtn = within(screen.getByTestId("ui-standard-btn-pg-preview")).getByRole("button");
    expect(previewBtn).toHaveClass("ui-standards-qbtn--outline");
    expect(previewBtn).toHaveStyle({ color: "#ef4444" });
    expect(previewBtn).toHaveTextContent("Standard");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn danger outline",
    );
    const shapeSelect = screen.getByTestId("ui-standard-btn-pg-shape");
    expect(within(shapeSelect).getAllByRole("option").map((option) => option.textContent)).toEqual([
      "Square",
      "Standard",
      "Soft",
      "Pill",
    ]);
    await user.selectOptions(shapeSelect, "square");
    expect(previewBtn).toHaveAttribute("data-shape", "square");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn danger outline square",
    );
    expect(screen.queryByTestId("ui-standard-btn-pg-size")).not.toBeInTheDocument();
    const density = screen.getByTestId("ui-standard-btn-pg-density");
    expect(within(density).getAllByRole("option").map((option) => option.textContent)).toEqual([
      "Tight",
      "Compact",
      "Standard",
      "Comfort",
      "Relaxed",
    ]);
    await user.selectOptions(density, "tight");
    expect(screen.getByTestId("ui-standard-btn-pg-preview")).toHaveAttribute("data-density", "tight");
    await user.selectOptions(density, "relaxed");
    expect(screen.getByTestId("ui-standard-btn-pg-preview")).toHaveAttribute("data-density", "relaxed");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn danger outline square relaxed",
    );
    const icon = screen.getByTestId("ui-standard-btn-pg-icon");
    expect(within(icon).getAllByRole("option").map((option) => option.textContent)?.[0]).toBe("None");
    expect(within(icon).getAllByRole("option")).toHaveLength(11);
    await user.selectOptions(icon, "star");
    expect(screen.getByTestId("ui-standard-btn-pg-preview-icon")).toBeInTheDocument();
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn danger outline square relaxed star",
    );
    await user.selectOptions(icon, "none");
    expect(screen.queryByTestId("ui-standard-btn-pg-preview-icon")).not.toBeInTheDocument();
    expect(previewBtn).toHaveTextContent("Standard");
    const labelInput = screen.getByTestId("ui-standard-btn-pg-label");
    await user.clear(labelInput);
    await user.type(labelInput, "SAVE");
    expect(previewBtn).toHaveTextContent("SAVE");
    await user.clear(labelInput);
    await user.type(labelInput, "save");
    expect(previewBtn).toHaveTextContent("save");
    await user.click(screen.getByTestId("ui-standard-btn-pg-icon-only"));
    expect(previewBtn).not.toHaveTextContent("Standard");
    expect(screen.getByTestId("ui-standard-btn-pg-preview-icon")).toBeInTheDocument();
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn icon danger outline square relaxed",
    );
    expect(screen.getByTestId("ui-standard-btn-pg-ripple")).toBeInTheDocument();
    expect(previewBtn).toHaveAttribute("data-ripple", "no-ripple");
    expect(screen.getByText("No ripple")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-btn-pg-ripple"));
    expect(previewBtn).toHaveAttribute("data-ripple", "ripple");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn icon danger outline square relaxed ripple",
    );
    await user.click(previewBtn);
    expect(screen.getByTestId("ui-standard-btn-pg-ripple-wave")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-btn-pg-disabled"));
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn icon danger outline square relaxed ripple disabled",
    );
    await user.click(screen.getByTestId("ui-standard-btn-quasar-standard-row-primary"));
    expect(screen.getByTestId("ui-standard-qbtn-ripple-wave")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-btn-pg-reset"));
    expect(screen.getByTestId("ui-standard-btn-pg-intent")).toHaveValue("standard");
    expect(screen.getByTestId("ui-standard-btn-pg-appearance")).toHaveValue("standard");
    expect(screen.getByTestId("ui-standard-btn-pg-shape")).toHaveValue("standard");
    expect(screen.getByTestId("ui-standard-btn-pg-density")).toHaveValue("balance");
    expect(screen.getByTestId("ui-standard-btn-pg-icon")).toHaveValue("none");
    expect(labelInput).toHaveValue("Standard");
    expect(previewBtn).toHaveTextContent("Standard");
    expect(previewBtn).toHaveAttribute("data-ripple", "no-ripple");
    expect(previewBtn).toHaveAttribute("data-shape", "standard");
    expect(screen.getByTestId("ui-standard-btn-pg-preview")).toHaveAttribute("data-density", "balance");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn",
    );
    expect(screen.queryByTestId("ui-standard-btn-pg-preview-icon")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-btn-pg-copy")).not.toBeInTheDocument();
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
    expect(within(preview).getByText("Global preview")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-main")).toContainElement(preview);
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

  it("toggles Global Preference Preview and offers back to top after scroll", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-buttons"));
    expect(screen.queryByTestId("ui-standards-preference-preview")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standards-filter-all"));
    expect(screen.getByTestId("ui-standards-preference-preview")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-preference-auto-samples")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standards-preference-preview-toggle"));
    expect(screen.queryByTestId("ui-standards-preference-auto-samples")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standards-preference-preview-toggle"));
    expect(screen.getByTestId("ui-standards-preference-auto-samples")).toBeInTheDocument();

    const main = screen.getByTestId("ui-standards-main");
    expect(screen.queryByTestId("ui-standards-scroll-top")).not.toBeInTheDocument();
    Object.defineProperty(main, "scrollTop", { configurable: true, get: () => 120 });
    main.dispatchEvent(new Event("scroll"));
    expect(await screen.findByTestId("ui-standards-scroll-top")).toHaveAttribute(
      "aria-label",
      "Back to top",
    );
  });

  it("does not show the removed Do / Don’t card", () => {
    renderPage();
    expect(screen.queryByTestId("ui-standard-card-dodont")).not.toBeInTheDocument();
    expect(screen.queryByText("Standards catalog")).not.toBeInTheDocument();
  });

  it("keeps playground, status, and select samples without a page search", () => {
    renderPage();
    expect(screen.queryByTestId("ui-standards-search")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-buttons")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-status")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-selects")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-dodont")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-preference-preview")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-button-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-status-playground")).toBeInTheDocument();
  });

  it("shows Soft Outline Solid appearances on the Status live card", () => {
    renderPage();
    const treatments = screen.getByTestId("ui-standard-card-status-treatments");
    expect(within(treatments).getAllByText("Appearance").length).toBeGreaterThanOrEqual(1);
    expect(within(treatments).getByTestId("ui-standard-status-appearance-soft")).toBeInTheDocument();
    expect(within(treatments).getByTestId("ui-standard-status-appearance-emphasis")).toBeInTheDocument();
    expect(within(treatments).getByTestId("ui-standard-status-appearance-outline")).toBeInTheDocument();
    expect(within(treatments).getByTestId("ui-standard-status-appearance-solid")).toBeInTheDocument();
    expect(within(treatments).getAllByText("Soft").length).toBeGreaterThanOrEqual(1);
    expect(within(treatments).getAllByText("Emphasis").length).toBeGreaterThanOrEqual(1);
    expect(within(treatments).getAllByText("Outline").length).toBeGreaterThanOrEqual(2);
    expect(within(treatments).getAllByText("Solid").length).toBeGreaterThanOrEqual(2);
    expect(treatments.querySelectorAll('[data-appearance="emphasis"]').length).toBeGreaterThan(0);
    expect(treatments.querySelectorAll('[data-appearance="solid"]').length).toBeGreaterThan(0);
    expect(treatments.querySelectorAll('[data-appearance="outline"]').length).toBeGreaterThan(0);
  });

  it("shows Auto and explicit Control Shape samples on the Status live card", () => {
    renderPage();
    const treatments = screen.getByTestId("ui-standard-card-status-treatments");
    expect(within(treatments).getAllByText("Shape").length).toBeGreaterThanOrEqual(1);
    expect(within(treatments).getByTestId("ui-standard-status-shape-auto-soft")).toHaveAttribute(
      "data-shape",
      "auto",
    );
    expect(within(treatments).getByTestId("ui-standard-status-shape-standard-outline")).toHaveAttribute(
      "data-shape",
      "standard",
    );
    expect(within(treatments).getByTestId("ui-standard-status-shape-soft-solid")).toHaveAttribute(
      "data-shape",
      "soft",
    );
    expect(within(treatments).getByTestId("ui-standard-status-shape-pill-soft")).toHaveAttribute(
      "data-shape",
      "pill",
    );
    expect(within(treatments).getByTestId("ui-standard-status-shape-square-soft")).toHaveAttribute(
      "data-shape",
      "square",
    );
    expect(within(treatments).getByTestId("ui-standard-status-shape-grid").className).toMatch(
      /repeat\(4/,
    );
    expect(
      within(treatments).getByText(/Auto follows the global Control Shape preference/, {
        exact: false,
      }),
    ).toBeInTheDocument();
  });

  it("shows Soft and Emphasis tone color variations on the Status live card", () => {
    renderPage();
    const status = screen.getByTestId("ui-standard-card-status");
    expect(within(status).getByTestId("ui-standard-status-tone-colors")).toBeInTheDocument();
    expect(within(status).getByTestId("ui-standard-status-tone-soft-primary")).toHaveAttribute(
      "data-tone",
      "primary",
    );
    expect(within(status).getByTestId("ui-standard-status-tone-soft-info")).toHaveAttribute(
      "data-appearance",
      "soft",
    );
    expect(within(status).getByTestId("ui-standard-status-tone-emphasis-primary")).toHaveAttribute(
      "data-appearance",
      "emphasis",
    );
    expect(within(status).getByTestId("ui-standard-status-tone-emphasis-info")).toHaveAttribute(
      "data-tone",
      "info",
    );
    expect(within(status).getByTestId("ui-standard-status-square-primary")).toHaveAttribute(
      "data-shape",
      "square",
    );
    expect(within(status).getByTestId("ui-standard-status-square-info")).toHaveAttribute(
      "data-appearance",
      "emphasis",
    );
    expect(within(status).getByTestId("ui-standard-status-square-warning")).toHaveAttribute(
      "data-tone",
      "warning",
    );
  });

  it("shows accent and metric card color variations on the Cards live card", () => {
    renderPage("/ui-standards?category=cards");

    const cards = screen.getByTestId("ui-standard-card-cards");
    expect(within(cards).getByTestId("ui-standard-card-accent-start-primary")).toHaveAttribute(
      "data-treatment",
      "accent",
    );
    expect(within(cards).getByTestId("ui-standard-card-accent-start-info")).toBeInTheDocument();
    expect(within(cards).getByTestId("ui-standard-card-accent-tint-primary")).toBeInTheDocument();
    expect(within(cards).getByTestId("ui-standard-card-accent-tint-info")).toBeInTheDocument();
    expect(within(cards).getByTestId("ui-standard-card-metric-emphasis")).toBeInTheDocument();
    expect(within(cards).getByTestId("ui-standard-card-metric-shared")).toBeInTheDocument();
    expect(within(cards).getByTestId("ui-standard-card-metric-attention")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-buttons")).not.toBeInTheDocument();
  });

  it("toggles selectable Cards selection on click", async () => {
    const user = userEvent.setup();
    renderPage("/ui-standards?category=cards");

    const main = screen.getByTestId("ui-standard-card-selectable-main");
    const iloilo = screen.getByTestId("ui-standard-card-selectable-iloilo");
    expect(main).toHaveAttribute("aria-checked", "true");
    expect(main.className).toMatch(/exits-card--selected/);
    expect(iloilo).toHaveAttribute("aria-checked", "false");

    await user.click(iloilo);
    expect(iloilo).toHaveAttribute("aria-checked", "true");
    expect(iloilo).toHaveAttribute("data-selected", "true");
    expect(iloilo.className).toMatch(/exits-card--selected/);
    expect(main).toHaveAttribute("aria-checked", "false");
    expect(main).not.toHaveAttribute("data-selected");
  });

  it("shows distinct treatment samples including Selected and Featured", () => {
    renderPage("/ui-standards?category=cards");

    const treatments = screen.getByTestId("ui-standard-card-treatments");
    expect(within(treatments).getByTestId("ui-standard-card-treatment-surface")).toHaveAttribute(
      "data-treatment",
      "surface",
    );
    expect(within(treatments).getByTestId("ui-standard-card-treatment-bordered")).toHaveAttribute(
      "data-treatment",
      "bordered",
    );
    expect(within(treatments).getByTestId("ui-standard-card-treatment-elevated")).toHaveAttribute(
      "data-treatment",
      "elevated",
    );
    expect(within(treatments).getByTestId("ui-standard-card-treatment-interactive")).toHaveAttribute(
      "data-treatment",
      "interactive",
    );
    expect(within(treatments).getByTestId("ui-standard-card-treatment-selected")).toHaveAttribute(
      "data-treatment",
      "selected",
    );
    expect(within(treatments).getByTestId("ui-standard-card-treatment-featured")).toHaveAttribute(
      "data-treatment",
      "featured",
    );
  });

  it("filters live cards by category", () => {
    renderPage("/ui-standards?category=overlays");
    expect(screen.getByTestId("ui-standard-card-drawer")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-modal")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-confirm")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-buttons")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-catalog-table")).not.toBeInTheDocument();

    cleanup();
    renderPage("/ui-standards?category=selects");
    expect(screen.getByTestId("ui-standard-card-selects")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-select-standard")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-select-multi")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-select-switch")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-forms")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-catalog-row-exits-select")).not.toBeInTheDocument();

    cleanup();
    renderPage("/ui-standards?category=messages");
    expect(screen.getByTestId("ui-standard-card-messages")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-toasts")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-table")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-catalog-row-message-gallery")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-catalog-row-toast")).not.toBeInTheDocument();
  });

  it("Navigation filter surfaces underline ExitsTabs and ModuleSubnav samples", () => {
    renderPage("/ui-standards?category=navigation");

    const nav = screen.getByTestId("ui-standard-card-nav");
    expect(nav).toBeInTheDocument();
    expect(within(nav).getByTestId("ui-standard-tabs-underline")).toBeInTheDocument();
    expect(within(nav).getByTestId("ui-standard-tabs-underline-icons")).toBeInTheDocument();
    expect(within(nav).getByTestId("ui-standard-tabs-underline-counts")).toBeInTheDocument();
    expect(within(nav).getByTestId("ui-standard-subnav-underline")).toBeInTheDocument();
    expect(within(nav).getByTestId("ui-standard-subnav-underline-icons")).toBeInTheDocument();
    expect(within(nav).getByText("Underline / Soft / Pill tabs")).toBeInTheDocument();
    expect(within(nav).getByText("Underline — module destinations")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-buttons")).not.toBeInTheDocument();
  });

  it("Data filter surfaces the locked ExitsTable reference demo", async () => {
    const user = userEvent.setup();
    renderPage("/ui-standards?category=data");

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

  it("Responsive Data View locks search, sort, export, page size, and category filter as default", () => {
    renderPage("/ui-standards?category=data");


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
    renderPage("/ui-standards?category=data");

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
    renderPage("/ui-standards?category=data");

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

  it("shows the responsive data preview on the Data category", () => {
    renderPage("/ui-standards?category=data");
    expect(screen.queryByTestId("ui-standards-search")).not.toBeInTheDocument();
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
    expect(screen.queryByTestId("ui-standard-catalog-row-quantity-stepper")).not.toBeInTheDocument();
  });

  it("opens Buttons filter from category=buttons with full button gallery", () => {
    renderPage("/ui-standards?category=buttons");
    expect(screen.getByTestId("ui-standards-filter-buttons")).toHaveAttribute("data-selected", "true");
    expect(screen.queryByTestId("ui-standards-preference-preview")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-buttons")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-button-pilot-badge")).toHaveTextContent(
      "BUTTON STANDARD — APPROVED / LOCKED",
    );
    expect(screen.getByTestId("ui-standard-button-gallery")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-dodont")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-upload")).not.toBeInTheDocument();
  });

  it("opens Messages filter from category=messages with locked message gallery", () => {
    renderPage("/ui-standards?category=messages");
    expect(screen.getByTestId("ui-standard-card-messages")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-message-pilot-badge")).toHaveTextContent(
      "MESSAGE STANDARD — APPROVED / LOCKED",
    );
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
