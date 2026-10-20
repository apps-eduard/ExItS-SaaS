import { beforeEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { cleanup, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { catalogs } from "@/i18n/messages";
import { ShellUiStandardsButton } from "@/components/exits/ShellUiStandardsButton";
import { UiStandardsPage } from "@/features/ui-standards/UiStandardsPage";
import { hourFromClockDegrees, minuteFromClockDegrees } from "@/features/ui-standards/UiStandardsTimeGallery";
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

async function openPlaygroundDropdown(user: ReturnType<typeof userEvent.setup>, testId: string) {
  const root = screen.getByTestId(testId);
  const field = within(root).getByTestId(`${testId}-field`);
  if (field.getAttribute("aria-expanded") !== "true") {
    await user.click(field);
  }
  return root;
}

async function choosePlaygroundDropdown(
  user: ReturnType<typeof userEvent.setup>,
  testId: string,
  optionSlug: string,
) {
  const root = await openPlaygroundDropdown(user, testId);
  await user.click(within(root).getByTestId(`${testId}-option-${optionSlug}`));
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
    const soft = screen.getByTestId("ui-standard-btn-quasar-soft");
    expect(within(soft).getByText("Button soft")).toBeInTheDocument();
    expect(within(soft).getByRole("button", { name: "Primary" })).toHaveClass("ui-standards-qbtn--soft");
    expect(within(soft).getByRole("button", { name: "Primary" })).toHaveStyle({ color: "#1976d2" });
    const softOutline = screen.getByTestId("ui-standard-btn-quasar-soft-outline");
    expect(within(softOutline).getByText("Button soft outline")).toBeInTheDocument();
    expect(within(softOutline).getByRole("button", { name: "Primary" })).toHaveClass(
      "ui-standards-qbtn--soft-outline",
    );
    expect(within(softOutline).getByRole("button", { name: "Primary" })).toHaveStyle({ color: "#1976d2" });
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
    const buttonIcons = screen.getByTestId("ui-standard-btn-quasar-button-icons");
    expect(within(buttonIcons).getByText("Medallion")).toBeInTheDocument();
    expect(within(buttonIcons).getByRole("button", { name: "Primary" })).toHaveClass(
      "ui-standards-qbtn-icon-medallion",
    );
    expect(within(buttonIcons).getByRole("button", { name: "Danger" })).toHaveClass(
      "ui-standards-qbtn-icon-medallion",
    );
    expect(roundGhost.compareDocumentPosition(buttonIcons)).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
    expect(
      buttonIcons.compareDocumentPosition(screen.getByTestId("ui-standard-btn-quasar-round-icon")),
    ).toBe(Node.DOCUMENT_POSITION_FOLLOWING);
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
    const intentSelect = await openPlaygroundDropdown(user, "ui-standard-btn-pg-intent");
    const intentOptions = within(intentSelect).getAllByRole("option").map((option) => option.textContent);
    expect(intentOptions[0]).toBe("Standard");
    expect(intentOptions.at(-1)).toBe("Black");
    expect(intentOptions).toHaveLength(10);
    const appearanceSelect = await openPlaygroundDropdown(user, "ui-standard-btn-pg-appearance");
    const appearanceOptions = within(appearanceSelect)
      .getAllByRole("option")
      .map((option) => option.textContent);
    expect(appearanceOptions[0]).toBe("Standard");
    expect(appearanceOptions).toContain("Soft");
    expect(appearanceOptions).toContain("Soft outline");
    expect(appearanceOptions).toContain("Medallion");
    expect(appearanceOptions.at(-1)).toBe("Medallion");
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-appearance", "soft");
    expect(screen.getByTestId("ui-standard-btn-pg-preview-btn")).toHaveClass("ui-standards-qbtn--soft");
    expect(screen.getByTestId("ui-standard-btn-pg-preview-icon-only")).toHaveClass(
      "ui-standards-qbtn--icon-only",
      "ui-standards-qbtn--soft",
    );
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-appearance", "soft-outline");
    expect(screen.getByTestId("ui-standard-btn-pg-preview-btn")).toHaveClass("ui-standards-qbtn--soft-outline");
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-intent", "danger");
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-appearance", "outline");
    const previewBtn = screen.getByTestId("ui-standard-btn-pg-preview-btn");
    expect(previewBtn).toHaveClass("ui-standards-qbtn--outline");
    expect(previewBtn).toHaveStyle({ color: "#ef4444" });
    expect(previewBtn).toHaveTextContent("Standard");
    expect(screen.getByTestId("ui-standard-btn-pg-preview-icon-only")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-btn-pg-preview-icon-only-glyph")).toBeInTheDocument();
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn danger outline",
    );
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-appearance", "medallion");
    expect(screen.getByTestId("ui-standard-btn-pg-preview")).toHaveAttribute("data-appearance", "medallion");
    expect(screen.getByTestId("ui-standard-btn-pg-preview-btn")).toHaveClass("ui-standards-qbtn-icon-medallion");
    expect(screen.getByTestId("ui-standard-btn-pg-preview-icon-only")).toHaveClass(
      "ui-standards-qbtn-icon-medallion",
    );
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn icon danger medallion",
    );
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-appearance", "outline");
    const shapeSelect = await openPlaygroundDropdown(user, "ui-standard-btn-pg-shape");
    expect(within(shapeSelect).getAllByRole("option").map((option) => option.textContent)).toEqual([
      "Square",
      "Standard",
      "Soft",
      "Pill",
    ]);
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-shape", "square");
    expect(previewBtn).toHaveAttribute("data-shape", "square");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn danger outline square",
    );
    expect(screen.queryByTestId("ui-standard-btn-pg-size")).not.toBeInTheDocument();
    const density = await openPlaygroundDropdown(user, "ui-standard-btn-pg-density");
    expect(within(density).getAllByRole("option").map((option) => option.textContent)).toEqual([
      "Compact",
      "Standard",
      "Comfort",
    ]);
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-density", "compact");
    expect(screen.getByTestId("ui-standard-btn-pg-preview")).toHaveAttribute("data-density", "compact");
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-density", "comfort");
    expect(screen.getByTestId("ui-standard-btn-pg-preview")).toHaveAttribute("data-density", "comfort");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn danger outline square comfort",
    );
    const icon = await openPlaygroundDropdown(user, "ui-standard-btn-pg-icon");
    expect(within(icon).getAllByRole("option").map((option) => option.textContent)?.[0]).toBe("None");
    expect(within(icon).getAllByRole("option")).toHaveLength(11);
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-icon", "star");
    expect(screen.getByTestId("ui-standard-btn-pg-preview-icon")).toBeInTheDocument();
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn danger outline square comfort star",
    );
    await choosePlaygroundDropdown(user, "ui-standard-btn-pg-icon", "none");
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
      "ext btn icon danger outline square comfort",
    );
    expect(screen.getByTestId("ui-standard-btn-pg-ripple")).toBeInTheDocument();
    expect(previewBtn).toHaveAttribute("data-ripple", "no-ripple");
    expect(screen.getByText("No ripple")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-btn-pg-ripple"));
    expect(previewBtn).toHaveAttribute("data-ripple", "ripple");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn icon danger outline square comfort ripple",
    );
    await user.click(previewBtn);
    expect(screen.getByTestId("ui-standard-btn-pg-ripple-wave")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-btn-pg-disabled"));
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext btn icon danger outline square comfort ripple disabled",
    );
    await user.click(screen.getByTestId("ui-standard-btn-quasar-standard-row-primary"));
    expect(screen.getByTestId("ui-standard-qbtn-ripple-wave")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-btn-pg-reset"));
    expect(screen.getByTestId("ui-standard-btn-pg-intent-field")).toHaveTextContent("Standard");
    expect(screen.getByTestId("ui-standard-btn-pg-appearance-field")).toHaveTextContent("Standard");
    expect(screen.getByTestId("ui-standard-btn-pg-shape-field")).toHaveTextContent("Standard");
    expect(screen.getByTestId("ui-standard-btn-pg-density-field")).toHaveTextContent("Standard");
    expect(screen.getByTestId("ui-standard-btn-pg-icon-field")).toHaveTextContent("None");
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
    await choosePlaygroundDropdown(user, "ui-standard-status-pg-tone", "warn");
    await choosePlaygroundDropdown(user, "ui-standard-status-pg-appearance", "solid");
    await choosePlaygroundDropdown(user, "ui-standard-status-pg-shape", "pill");
    const chip = within(screen.getByTestId("ui-standard-status-pg-preview")).getByText("Active");
    expect(chip).toHaveAttribute("data-tone", "warning");
    expect(chip).toHaveAttribute("data-appearance", "solid");
    expect(chip).toHaveAttribute("data-shape", "pill");
    expect(screen.getByTestId("ui-standard-status-pg-snippet")).toHaveTextContent('tone="warning"');
    expect(screen.getByTestId("ui-standard-status-shape-auto-soft")).toBeInTheDocument();
  });

  it("does not show the removed ExitsSelect playground card", () => {
    renderPage();
    expect(screen.queryByTestId("ui-standard-card-select-api")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-select-playground")).not.toBeInTheDocument();
  });

  it("wires Global Preference Preview to the real preference store", async () => {
    const user = userEvent.setup();
    renderPage();
    const preview = screen.getByTestId("ui-standards-preference-preview");
    expect(within(preview).getByText("Global preview")).toBeInTheDocument();
    expect(within(preview).getByTestId("preferences-primary-green")).toHaveAttribute("data-selected", "true");
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
    expect(screen.queryByTestId("ui-standard-card-selects")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-dropdown-playground")).toBeInTheDocument();
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

  it("opens Card under Cards and keeps the receipt on Card receipt", async () => {
    const user = userEvent.setup();
    renderPage();
    const group = screen.getByTestId("ui-standards-filter-cards");
    expect(group).toHaveTextContent("Cards");
    expect(group).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByTestId("ui-standards-filter-card")).toHaveTextContent("Card");
    expect(screen.getByTestId("ui-standards-filter-card-receipt")).toHaveTextContent("Card receipt");
    await user.click(screen.getByTestId("ui-standards-filter-card"));
    expect(screen.getByTestId("ui-standard-card-card-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-pg-tab-card")).toHaveAttribute("aria-selected", "true");
    expect(screen.getByTestId("ui-standard-card-pg-card-settings")).toHaveTextContent("Card");
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-header"));
    expect(screen.getByTestId("ui-standard-card-pg-header-settings")).toHaveTextContent("Header");
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-body"));
    expect(screen.getByTestId("ui-standard-card-pg-body-settings")).toHaveTextContent("Body");
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-footer"));
    expect(screen.getByTestId("ui-standard-card-pg-footer-settings")).toHaveTextContent("Footer");
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-card"));
    const previewSlot = screen.getByTestId("ui-standard-card-pg-preview-slot");
    expect(previewSlot).toContainElement(screen.getByTestId("ui-standard-card-pg-sample"));
    expect(screen.getByTestId("ui-standard-card-pg-preview")).toContainElement(previewSlot);
    expect(screen.getByTestId("ui-standard-card-card-playground")).toHaveTextContent("Preview");
    expect(screen.getByTestId("ui-standard-card-pg-reset")).toHaveAttribute("aria-label", "Reset");
    expect(screen.getByTestId("ui-standard-card-pg-theme")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-intent", "standard");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-elevation", "3");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-radius", "1");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-border-sides", "");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveTextContent("Our Changing Planet");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-intent", "standard");
    expect(screen.getByTestId("ui-standard-card-pg-body-part")).toHaveTextContent("Lorem ipsum");
    expect(screen.queryByTestId("ui-standard-card-pg-footer-part")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card");
    expect(screen.getByTestId("ui-standard-card-cards")).toHaveTextContent("Card samples");
    expect(screen.getByTestId("ui-standard-card-ribbon-sample")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-banner-heading")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-pennant-heading")).toHaveTextContent("Card pennant");
    expect(screen.getByTestId("ui-standard-card-pennant-blue")).toHaveAttribute("data-intent", "blue");
    expect(screen.getByTestId("ui-standard-card-pennant-purple")).toHaveAttribute("data-intent", "purple");
    expect(screen.getByTestId("ui-standard-card-pennant-pink")).toHaveAttribute("data-intent", "pink");
    expect(screen.getByTestId("ui-standard-card-pennant-amber")).toHaveAttribute("data-intent", "amber");
    expect(screen.getAllByTestId(/^ui-standard-card-pennant-(blue|purple|pink|amber|info|success|danger|warn)$/)).toHaveLength(4);
    expect(screen.queryByTestId("ui-standard-card-pennant-info")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-pennant-ribbon-blue")).toHaveAttribute("aria-label", "Primary");
    expect(screen.getByTestId("ui-standard-card-pennant-ribbon-purple")).toHaveAttribute("aria-label", "Secondary");
    expect(screen.getByTestId("ui-standard-card-pennant-body-blue")).toHaveTextContent("Counter goals");
    expect(screen.getByTestId("ui-standard-card-pennant-body-blue")).toHaveTextContent("Primary");
    expect(screen.getByTestId("ui-standard-card-pennant-footer-blue")).toHaveTextContent("Open board");
    expect(screen.getByTestId("ui-standard-card-pennant-footer-amber")).toHaveTextContent("Share flyer");
    expect(screen.getByTestId("ui-standard-card-capsule-heading")).toHaveTextContent("Card true capsule");
    expect(screen.getByTestId("ui-standard-card-capsule-magenta")).toHaveAttribute("data-intent", "magenta");
    expect(screen.getByTestId("ui-standard-card-capsule-info")).toHaveAttribute("data-intent", "info");
    expect(screen.getByTestId("ui-standard-card-capsule-success")).toHaveAttribute("data-intent", "success");
    expect(screen.getByTestId("ui-standard-card-capsule-warn")).toHaveAttribute("data-intent", "warn");
    expect(screen.getByTestId("ui-standard-card-capsule-ring-magenta")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-capsule-body-magenta")).toHaveTextContent("Lorem Ipsum");
    expect(screen.getByTestId("ui-standard-card-quick-action-heading")).toHaveTextContent("Card quick action");
    expect(screen.getByTestId("ui-standard-card-quick-action-info")).toHaveTextContent("Money I owe");
    expect(screen.getByTestId("ui-standard-card-quick-action-secondary")).toHaveTextContent("Add person");
    expect(screen.getByTestId("ui-standard-card-welcome-heading")).toHaveTextContent("Card welcome");
    expect(screen.getByTestId("ui-standard-card-page-header-heading")).toHaveTextContent("Card page header");
    expect(screen.getByTestId("ui-standard-card-page-header-primary")).toHaveTextContent("Money I owe");
    expect(screen.getByTestId("ui-standard-card-welcome-primary")).toHaveTextContent("Paul Uy");
    expect(screen.getByTestId("ui-standard-card-avatar-row-heading")).toHaveTextContent("Card avatar row");
    expect(screen.getByTestId("ui-standard-card-avatar-row-warn")).toHaveAttribute("data-intent", "warn");
    expect(screen.getByTestId("ui-standard-card-avatar-row-warn")).toHaveTextContent("Mica Uy — payment overdue");
    expect(screen.getByTestId("ui-standard-card-ribbon-heading")).toHaveTextContent("Card ribbon");
    expect(screen.queryByTestId("ui-standard-card-title-tab-heading")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-option-heading")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-infodata-heading")).toHaveTextContent("Card infodata");
    expect(screen.getByTestId("ui-standard-card-infodata-magenta")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-infodata-icon-magenta")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-infodata-body-magenta")).toHaveTextContent("Infodata 01");
    expect(screen.getByTestId("ui-standard-card-idea-heading")).toHaveTextContent("Card idea");
    expect(screen.getByTestId("ui-standard-card-idea-coral")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-idea-label-coral")).toHaveTextContent("Idea");
    expect(screen.getByTestId("ui-standard-card-idea-icon-coral")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-idea-body-coral")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-settings-heading")).toHaveTextContent("Card settings");
    expect(screen.getByTestId("ui-standard-card-settings-magenta")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-settings-title-magenta")).toHaveTextContent("Settings");
    expect(screen.getByTestId("ui-standard-card-settings-icon-magenta")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-settings-body-magenta")).toHaveTextContent(
      "Customize tools and systems",
    );
    expect(screen.getByTestId("ui-standard-card-pin-heading")).toHaveTextContent("Card pin");
    expect(screen.getByTestId("ui-standard-card-pin-amber")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-pin-icon-amber")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-pin-ribbon-amber")).toHaveTextContent("Lorem Ipsum");
    expect(screen.getByTestId("ui-standard-card-pin-body-amber")).toHaveTextContent("Read more");
    expect(screen.queryByTestId("ui-standard-card-fold-heading")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-index-heading")).toHaveTextContent("Card index");
    expect(screen.getByTestId("ui-standard-card-index-info")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-index-icon-info")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-index-title-info")).toHaveTextContent(
      "Lorem ipsum dolor sit amet",
    );
    expect(screen.getByTestId("ui-standard-card-index-badge-info")).toHaveTextContent("01");
    expect(screen.getByTestId("ui-standard-card-index-body-info")).toHaveTextContent("Lorem ipsum");
    expect(screen.getByTestId("ui-standard-card-step-heading")).toHaveTextContent("Card step");
    expect(screen.getByTestId("ui-standard-card-step-warn")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-step-icon-warn")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-step-rail-warn")).toHaveTextContent("Step 01");
    expect(screen.getByTestId("ui-standard-card-step-title-warn")).toHaveTextContent("Your title here");
    expect(screen.getByTestId("ui-standard-card-step-body-warn")).toHaveTextContent("Lorem ipsum");
    expect(screen.getByTestId("ui-standard-card-process-heading")).toHaveTextContent("Card process");
    expect(screen.getByTestId("ui-standard-card-process-amber")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-process-icon-info")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-process-title-coral")).toHaveTextContent("Sample Text");
    expect(screen.getByTestId("ui-standard-card-process-body-lime")).toHaveTextContent("Lorem ipsum");
    expect(screen.getByTestId("ui-standard-card-arrow-heading")).toHaveTextContent("Card arrow");
    expect(screen.getByTestId("ui-standard-card-arrow-violet")).toHaveTextContent("01");
    expect(screen.getByTestId("ui-standard-card-arrow-icon-green")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-arrow-title-coral")).toHaveTextContent("Sample Text");
    expect(screen.getByTestId("ui-standard-card-arrow-body-amber")).toHaveTextContent("Lorem ipsum");
    expect(screen.getByTestId("ui-standard-card-ribbon-pendant-heading")).toHaveTextContent(
      "Card ribbon pendant",
    );
    expect(screen.getByTestId("ui-standard-card-ribbon-pendant-primary")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-ribbon-pendant-info")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-ribbon-pendant-warn")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-ribbon-pendant-success")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-ribbon-pendant-icon-primary")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-ribbon-pendant-label-primary")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-ribbon-pendant-body-primary")).toHaveTextContent(
      "Our Changing Planet",
    );
    expect(screen.getByTestId("ui-standard-card-ribbon-label-primary")).toHaveTextContent("Primary");
    expect(screen.getByTestId("ui-standard-card-ribbon-label-secondary")).toHaveTextContent("Secondary");
    expect(screen.getByTestId("ui-standard-card-ribbon-label-info")).toHaveTextContent("Info");
    expect(screen.getByTestId("ui-standard-card-ribbon-label-warning")).toHaveTextContent("Warning");
    expect(screen.getByTestId("ui-standard-card-ribbon-primary")).toHaveAttribute("data-intent", "primary");
    expect(screen.getByTestId("ui-standard-card-ribbon-primary")).toHaveAttribute("data-appearance", "raised");
    expect(screen.getByTestId("ui-standard-card-ribbon-secondary")).toHaveAttribute("data-intent", "secondary");
    expect(screen.getByTestId("ui-standard-card-ribbon-secondary")).toHaveAttribute("data-appearance", "gradient");
    expect(screen.getByTestId("ui-standard-card-ribbon-info")).toHaveAttribute("data-intent", "info");
    expect(screen.getByTestId("ui-standard-card-ribbon-info")).toHaveAttribute("data-appearance", "glossy");
    expect(screen.getByTestId("ui-standard-card-ribbon-warning")).toHaveAttribute("data-intent", "warning");
    expect(screen.getByTestId("ui-standard-card-ribbon-warning")).toHaveAttribute("data-appearance", "raised");
    expect(screen.getByTestId("ui-standard-card-ribbon-label-primary")).toHaveAttribute("data-appearance", "raised");
    expect(screen.getByTestId("ui-standard-card-ribbon-label-secondary")).toHaveAttribute("data-appearance", "gradient");
    expect(screen.getByTestId("ui-standard-card-ribbon-label-info")).toHaveAttribute("data-appearance", "glossy");
    expect(screen.getByTestId("ui-standard-card-ribbon-label-warning")).toHaveAttribute("data-appearance", "raised");
    expect(screen.getByTestId("ui-standard-card-ribbon-body-primary")).toHaveTextContent("Our Changing Planet");
    expect(screen.getByTestId("ui-standard-card-ribbon-body-primary")).toHaveTextContent("by John Doe");
    expect(screen.getByTestId("ui-standard-card-ticket-heading")).toHaveTextContent("Card ticket");
    expect(screen.getByTestId("ui-standard-card-ticket-stub")).toHaveTextContent("A-17");
    expect(screen.getByTestId("ui-standard-card-ticket-body")).toHaveTextContent("Evening Market Pass");
    expect(screen.getByTestId("ui-standard-card-samples-grid")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-samples-grid")).toContainElement(
      screen.getByTestId("ui-standard-card-ribbon-sample"),
    );
    expect(screen.getByTestId("ui-standard-card-samples-grid")).toContainElement(
      screen.getByTestId("ui-standard-card-stamp-sample"),
    );
    expect(screen.getByTestId("ui-standard-card-bookmark-heading")).toHaveTextContent("Card bookmark");
    expect(screen.getByTestId("ui-standard-card-bookmark-tab")).toHaveTextContent("Saved");
    expect(screen.getByTestId("ui-standard-card-stamp-heading")).toHaveTextContent("Card stamp");
    expect(screen.getByTestId("ui-standard-card-stamp-seal")).toHaveTextContent("OK");
    expect(screen.queryByTestId("ui-standard-card-stack-heading")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-stripe-heading")).toHaveTextContent("Card stripe");
    expect(screen.getByTestId("ui-standard-card-stripe-band")).toHaveTextContent("New");
    expect(screen.getByTestId("ui-standard-card-notch-heading")).toHaveTextContent("Card notch");
    expect(screen.getByTestId("ui-standard-card-notch-chip")).toHaveTextContent("JD");
    expect(screen.getByTestId("ui-standard-card-clip-heading")).toHaveTextContent("Card clip");
    expect(screen.getByTestId("ui-standard-card-clip-body")).toHaveTextContent("Attached memo");
    expect(screen.queryByTestId("ui-standard-card-footer-actions-heading")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-footer-caption-heading")).toHaveTextContent("Card caption");
    expect(screen.getByTestId("ui-standard-card-footer-caption-bar")).toHaveTextContent("Updated 2 min ago");
    expect(screen.getByTestId("ui-standard-card-footer-success-bar")).toHaveTextContent("Settled · cash");
    expect(screen.queryByTestId("ui-standard-card-footer-split-heading")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-invoice-sample")).not.toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-card", "primary");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-intent", "primary");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-fill", "gradient");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveClass("ui-standards-qcard--gradient");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-gradient", "to-bottom");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary gradient");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-gradient", "left-to-right");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-gradient", "to-right");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary gradient to-right");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-gradient", "center");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-gradient", "center");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary gradient center");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-fill", "solid");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).not.toHaveClass("ui-standards-qcard--gradient");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary");
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-header"));
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-header-intent", "secondary");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-intent", "secondary");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary header-secondary");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-header", "none");
    expect(screen.queryByTestId("ui-standard-card-pg-header-part")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary no-header");
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-body"));
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-body", "two-paragraphs");
    expect(screen.getByTestId("ui-standard-card-pg-body-part")).toHaveTextContent("Sed do eiusmod");
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-footer"));
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-footer", "actions");
    expect(screen.getByTestId("ui-standard-card-pg-footer-part")).toHaveTextContent("Action 1");
    expect(screen.getByTestId("ui-standard-card-pg-footer-part")).toHaveAttribute("data-align", "start");
    expect(screen.getByTestId("ui-standard-card-pg-footer-part")).toHaveAttribute("data-appearance", "standard");
    expect(screen.getByTestId("ui-standard-card-pg-footer-part")).toHaveAttribute("data-density", "balance");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-footer-appearance", "soft");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-footer-density", "compact");
    expect(screen.getByTestId("ui-standard-card-pg-footer-part")).toHaveAttribute("data-appearance", "soft");
    expect(screen.getByTestId("ui-standard-card-pg-footer-part")).toHaveAttribute("data-density", "compact");
    expect(within(screen.getByTestId("ui-standard-card-pg-footer-part")).getByRole("button", { name: "Action 1" })).toHaveClass(
      "ui-standards-qbtn--soft",
    );
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary no-header sections actions btn-soft btn-compact",
    );
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-footer-appearance", "standard");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-footer-density", "standard");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary no-header sections actions");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary no-header sections actions");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-footer-align", "center");
    expect(screen.getByTestId("ui-standard-card-pg-footer-part")).toHaveAttribute("data-align", "center");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary no-header sections actions center");
    await user.click(screen.getByTestId("ui-standard-card-pg-footer-icons"));
    const iconFooter = screen.getByTestId("ui-standard-card-pg-footer-part");
    expect(iconFooter).toHaveClass("ui-standards-qcard__footer--icons");
    expect(within(iconFooter).getByRole("button", { name: "Event" })).toBeInTheDocument();
    expect(within(iconFooter).getByRole("button", { name: "Share" })).toBeInTheDocument();
    expect(iconFooter).not.toHaveTextContent("Action 1");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card primary no-header sections icons center");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-footer-intent", "success");
    expect(screen.getByTestId("ui-standard-card-pg-footer-part")).toHaveAttribute("data-intent", "success");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary no-header sections icons center footer-success",
    );
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-header"));
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-header", "title");
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-footer"));
    await user.click(screen.getByTestId("ui-standard-card-pg-footer-vertical"));
    const verticalFooter = screen.getByTestId("ui-standard-card-pg-footer-part");
    const sample = screen.getByTestId("ui-standard-card-pg-sample");
    const title = screen.getByTestId("ui-standard-card-pg-header-part");
    expect(sample).toHaveClass("ui-standards-qcard--side");
    expect(title.parentElement).toBe(sample);
    expect(verticalFooter.parentElement).toHaveClass("ui-standards-qcard__body-row");
    expect(verticalFooter).toHaveClass("ui-standards-qcard__footer--vertical");
    expect(verticalFooter).toHaveAttribute("data-align", "center");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-footer-align", "end");
    expect(verticalFooter).toHaveAttribute("data-align", "end");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-footer-align", "center");
    expect(within(verticalFooter).getByRole("button", { name: "Event" })).toBeInTheDocument();
    expect(within(verticalFooter).getByRole("button", { name: "Share" })).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary header-secondary sections icons-right center footer-success",
    );
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-card"));
    const radiusSlider = screen.getByTestId("ui-standard-card-pg-radius");
    radiusSlider.focus();
    await user.keyboard("{End}");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-radius", "10");
    expect(screen.getByTestId("ui-standard-card-pg-radius")).toHaveAttribute("aria-valuenow", "10");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary radius-10 header-secondary sections icons-right center footer-success",
    );
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-corners", "lt,lb,rt,rb");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveStyle({ "--qcard-radius": "24px" });
    await user.click(screen.getByTestId("ui-standard-card-pg-radius-corners-lt"));
    await user.click(screen.getByTestId("ui-standard-card-pg-radius-corners-lb"));
    await user.click(screen.getByTestId("ui-standard-card-pg-radius-corners-rb"));
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-corners", "rt");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveStyle({ "--qcard-radius": "0 24px 0 0" });
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary radius-rt-10 header-secondary sections icons-right center footer-success",
    );
    await user.click(screen.getByTestId("ui-standard-card-pg-radius-corners-lt"));
    await user.click(screen.getByTestId("ui-standard-card-pg-radius-corners-lb"));
    await user.click(screen.getByTestId("ui-standard-card-pg-radius-corners-rb"));
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary radius-10 header-secondary sections icons-right center footer-success",
    );
    const elevationSlider = screen.getByTestId("ui-standard-card-pg-elevation");
    elevationSlider.focus();
    await user.keyboard("{Home}");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-elevation", "1");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveStyle({ "--qcard-shadow": "none" });
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary flat radius-10 header-secondary sections icons-right center footer-success",
    );
    await user.click(screen.getByTestId("ui-standard-card-pg-border-sides-l"));
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-border-sides", "l");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveStyle({ "--qcard-border-width": "0 0 0 1px" });
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary flat radius-10 border-l header-secondary sections icons-right center footer-success",
    );
    const borderSlider = screen.getByTestId("ui-standard-card-pg-border-size");
    borderSlider.focus();
    await user.keyboard("{End}");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-border", "10");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveStyle({ "--qcard-border-width": "0 0 0 10px" });
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary flat radius-10 border-l-10 header-secondary sections icons-right center footer-success",
    );
    await user.click(screen.getByTestId("ui-standard-card-pg-tab-header"));
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-title-align", "center");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-title-align", "center");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary flat radius-10 border-l-10 header-secondary title-center sections icons-right center footer-success",
    );
    const fontSlider = screen.getByTestId("ui-standard-card-pg-font");
    expect(within(screen.getByTestId("ui-standard-card-pg-header-settings")).getByTestId("ui-standard-card-pg-font")).toBe(fontSlider);
    expect(screen.queryByTestId("ui-standard-card-pg-card-settings")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-font", "4");
    expect(screen.getByTestId("ui-standard-card-pg-font-value")).toHaveTextContent("H4");
    fontSlider.focus();
    await user.keyboard("{Home}");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-font", "1");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveStyle({ "--qheader-font": "2rem" });
    expect(screen.getByTestId("ui-standard-card-pg-font-value")).toHaveTextContent("H1");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary flat radius-10 border-l-10 header-secondary title-center h1 sections icons-right center footer-success",
    );
    await user.keyboard("{End}");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-font", "5");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveStyle({ "--qheader-font": "1rem" });
    await user.keyboard("{ArrowLeft}");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-font", "4");
    const heightSlider = screen.getByTestId("ui-standard-card-pg-height");
    expect(within(screen.getByTestId("ui-standard-card-pg-header-settings")).getByTestId("ui-standard-card-pg-height")).toBe(heightSlider);
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-height", "3");
    const widthSlider = screen.getByTestId("ui-standard-card-pg-width");
    expect(within(screen.getByTestId("ui-standard-card-pg-header-settings")).getByTestId("ui-standard-card-pg-width")).toBe(widthSlider);
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-width", "3");
    heightSlider.focus();
    await user.keyboard("{Home}");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-height", "1");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveStyle({ "--qheader-pad": "0.125rem" });
    await user.keyboard("{End}");
    const headerPart = screen.getByTestId("ui-standard-card-pg-header-part");
    expect(headerPart).toHaveAttribute("data-height", "5");
    expect(headerPart).toHaveStyle({ "--qheader-min-height": "7rem" });
    expect(screen.getByTestId("ui-standard-card-pg-sample")).not.toHaveAttribute("data-height");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary flat radius-10 border-l-10 header-secondary title-center height-5 sections icons-right center footer-success",
    );
    widthSlider.focus();
    await user.keyboard("{ArrowLeft}");
    expect(headerPart).toHaveAttribute("data-width", "2");
    expect(headerPart).toHaveStyle({ "--qheader-inset": "0.75rem" });
    await user.keyboard("{ArrowRight}{ArrowRight}");
    expect(headerPart).toHaveAttribute("data-width", "4");
    expect(headerPart).toHaveStyle({ "--qheader-inset": "-0.75rem" });
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveClass("ui-standards-qcard--release");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary flat radius-10 border-l-10 header-secondary title-center height-5 width-4 sections icons-right center footer-success",
    );
    await user.keyboard("{ArrowLeft}");
    expect(headerPart).toHaveAttribute("data-width", "3");
    const headerRadius = screen.getByTestId("ui-standard-card-pg-header-radius");
    headerRadius.focus();
    await user.keyboard("{ArrowRight}");
    expect(headerPart).toHaveAttribute("data-radius", "2");
    expect(headerPart).toHaveStyle({ "--qheader-radius": "2px" });
    await user.click(screen.getByTestId("ui-standard-card-pg-header-radius-corners-lt"));
    await user.click(screen.getByTestId("ui-standard-card-pg-header-radius-corners-lb"));
    await user.click(screen.getByTestId("ui-standard-card-pg-header-radius-corners-rb"));
    expect(headerPart).toHaveAttribute("data-corners", "rt");
    expect(headerPart).toHaveStyle({ "--qheader-radius": "0 2px 0 0" });
    await user.click(screen.getByTestId("ui-standard-card-pg-header-radius-corners-lt"));
    await user.click(screen.getByTestId("ui-standard-card-pg-header-radius-corners-lb"));
    await user.click(screen.getByTestId("ui-standard-card-pg-header-radius-corners-rb"));
    expect(headerPart).toHaveStyle({ "--qheader-radius": "2px" });
    await user.click(screen.getByTestId("ui-standard-card-pg-header-border-sides-l"));
    expect(headerPart).toHaveAttribute("data-border-sides", "l");
    await user.click(screen.getByTestId("ui-standard-card-pg-header-pill"));
    expect(headerPart).toHaveAttribute("data-pill", "true");
    expect(headerPart).toHaveStyle({ "--qheader-radius": "9999px" });
    const headerElevation = screen.getByTestId("ui-standard-card-pg-header-elevation");
    headerElevation.focus();
    await user.keyboard("{End}");
    expect(headerPart).toHaveAttribute("data-elevation", "5");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary flat radius-10 border-l-10 header-secondary title-center header-pill header-raised-5 header-radius-2 header-border-l height-5 sections icons-right center footer-success",
    );
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-header-appearance", "gradient");
    expect(headerPart).toHaveAttribute("data-appearance", "gradient");
    await choosePlaygroundDropdown(user, "ui-standard-card-pg-header-appearance", "outline");
    expect(headerPart).toHaveAttribute("data-appearance", "outline");
    expect(headerPart).toHaveStyle({
      color: "#26a69a",
      "--qheader-border-width": "0 0 0 1px",
      "--qheader-border-color": "var(--qheader-accent)",
    });
    const positionSlider = screen.getByTestId("ui-standard-card-pg-header-position");
    positionSlider.focus();
    await user.keyboard("{ArrowRight}{ArrowRight}");
    expect(headerPart).toHaveAttribute("data-position", "3");
    expect(headerPart).toHaveStyle({ "--qheader-shift": "-50%" });
    await user.keyboard("{End}");
    expect(headerPart).toHaveAttribute("data-position", "5");
    expect(headerPart).toHaveStyle({ "--qheader-shift": "-100%" });
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveClass("ui-standards-qcard--release");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext card primary flat radius-10 border-l-10 header-secondary header-outline title-center header-pill header-position-5 header-raised-5 header-radius-2 header-border-l height-5 sections icons-right center footer-success",
    );
    await user.click(screen.getByTestId("ui-standard-card-pg-reset"));
    expect(screen.getByTestId("ui-standard-card-pg-tab-card")).toHaveAttribute("aria-selected", "true");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-intent", "standard");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-elevation", "3");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-radius", "1");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-border-sides", "");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-height", "3");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-width", "3");
    expect(screen.getByTestId("ui-standard-card-pg-sample")).toHaveAttribute("data-corners", "lt,lb,rt,rb");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-corners", "lt,lb,rt,rb");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-appearance", "standard");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-pill", "false");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-position", "1");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-elevation", "1");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-intent", "standard");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-title-align", "start");
    expect(screen.getByTestId("ui-standard-card-pg-header-part")).toHaveAttribute("data-font", "4");
    expect(screen.queryByTestId("ui-standard-card-pg-footer-part")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext card");
    const themeButton = screen.getByTestId("ui-standard-card-pg-theme");
    await user.click(themeButton);
    const theme = document.documentElement.getAttribute("data-theme");
    expect(theme === "light" || theme === "dark").toBe(true);
    expect(screen.getByTestId("ui-standard-card-pg-preview")).toHaveAttribute("data-preview-theme", theme);
    await user.click(themeButton);
    expect(document.documentElement.getAttribute("data-theme")).toBe(theme === "dark" ? "light" : "dark");
    window.localStorage.removeItem("exits.pos-client.ui-preferences.v1");
    document.documentElement.dataset.theme = "system";
    await user.click(screen.getByTestId("ui-standards-filter-card-receipt"));
    expect(screen.getByTestId("ui-standard-card-receipt")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-invoice-sample")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-card-playground")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-cards")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-selectable-main")).not.toBeInTheDocument();
  }, 20000);

  it("opens the Shadow playground", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-shadow"));
    expect(screen.getByTestId("ui-standard-card-shadow-playground")).toHaveTextContent("Shadow playground");
    expect(screen.getByTestId("ui-standard-shadow-pg-plate")).toHaveAttribute("data-elevation", "medium");
    expect(screen.getByTestId("ui-standard-shadow-pg-plate")).toHaveAttribute("data-inset", "false");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext shadow");
    expect(screen.queryByTestId("ui-standard-card-card-playground")).not.toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-shadow-pg-elevation", "large");
    expect(screen.getByTestId("ui-standard-shadow-pg-plate")).toHaveAttribute("data-elevation", "large");
    const elevationMenu = await openPlaygroundDropdown(user, "ui-standard-shadow-pg-elevation");
    const largeOption = within(elevationMenu).getByTestId("ui-standard-shadow-pg-elevation-option-large");
    expect(largeOption).toHaveAttribute("aria-selected", "true");
    expect(within(largeOption).getByTestId("ui-standard-shadow-pg-elevation-selected-check")).toBeInTheDocument();
    expect(
      within(within(elevationMenu).getByTestId("ui-standard-shadow-pg-elevation-option-small")).queryByTestId(
        "ui-standard-shadow-pg-elevation-selected-check",
      ),
    ).not.toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext shadow large");
    await user.click(screen.getByTestId("ui-standard-shadow-pg-inset"));
    expect(screen.getByTestId("ui-standard-shadow-pg-plate")).toHaveAttribute("data-inset", "true");
    expect(screen.getByTestId("ui-standard-shadow-pg-plate")).toHaveStyle({ boxShadow: "inset var(--exits-shadow-lg)" });
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext shadow large inset");
    await user.click(screen.getByTestId("ui-standard-shadow-pg-reset"));
    expect(screen.getByTestId("ui-standard-shadow-pg-plate")).toHaveAttribute("data-elevation", "medium");
    expect(screen.getByTestId("ui-standard-shadow-pg-plate")).toHaveAttribute("data-inset", "false");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext shadow");
  });

  it("nests Button, Button stepper, Button group, and Button dropdown under Buttons", async () => {
    const user = userEvent.setup();
    renderPage();
    const group = screen.getByTestId("ui-standards-filter-button-components");
    expect(group).toHaveTextContent("Buttons");
    expect(group).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByTestId("ui-standards-filter-buttons")).toHaveTextContent("Button");
    expect(screen.getByTestId("ui-standards-filter-button-stepper")).toHaveTextContent("Button stepper");
    expect(screen.getByTestId("ui-standards-filter-button-group")).toHaveTextContent("Button group");
    expect(screen.getByTestId("ui-standards-filter-button-dropdown")).toHaveTextContent("Button dropdown");

    await user.click(screen.getByTestId("ui-standards-filter-button-stepper"));
    expect(screen.getByTestId("ui-standard-card-quantity-stepper")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-buttons")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("ui-standards-filter-button-group"));
    expect(screen.getByTestId("ui-standard-card-button-group")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-quantity-stepper")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-button-group-solid-group")).toHaveAttribute("role", "group");
    await user.click(screen.getByTestId("ui-standard-button-group-align-center"));
    expect(screen.getByTestId("ui-standard-button-group-align-center")).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByTestId("ui-standard-button-group-align-left")).toHaveAttribute("aria-pressed", "false");
    await user.click(screen.getByTestId("ui-standard-button-group-mark-italic"));
    expect(screen.getByTestId("ui-standard-button-group-mark-italic")).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByTestId("ui-standard-button-group-mark-bold")).toHaveAttribute("aria-pressed", "true");

    await user.click(screen.getByTestId("ui-standards-filter-button-dropdown"));
    expect(screen.getByTestId("ui-standard-card-button-dropdown")).toBeInTheDocument();
    const trigger = screen.getByTestId("ui-standard-button-dropdown-basic-trigger");
    expect(trigger).toHaveAttribute("aria-expanded", "false");
    await user.click(trigger);
    expect(trigger).toHaveAttribute("aria-expanded", "true");
    await user.click(await screen.findByTestId("ui-standard-button-dropdown-item-duplicate"));
    expect(screen.getByTestId("ui-standard-button-dropdown-last")).toHaveTextContent("Last action: Duplicate");
    expect(trigger).toHaveAttribute("aria-expanded", "false");

    await user.click(screen.getByTestId("ui-standard-button-dropdown-split-trigger"));
    await user.click(await screen.findByTestId("ui-standard-button-dropdown-item-save-draft"));
    expect(screen.getByTestId("ui-standard-button-dropdown-last")).toHaveTextContent("Last action: Save as draft");
    await user.click(screen.getByTestId("ui-standard-button-dropdown-split-main"));
    expect(screen.getByTestId("ui-standard-button-dropdown-last")).toHaveTextContent("Last action: Save");
  });

  it("opens Tabs select with the Quasar basic tabs and manual keyboard activation", async () => {
    const user = userEvent.setup();
    renderPage();
    const item = screen.getByTestId("ui-standards-filter-tabs-select");
    expect(item).toHaveTextContent("Tabs select");
    expect(screen.getByRole("group", { name: "Selects" })).toContainElement(item);
    await user.click(item);
    const card = screen.getByTestId("ui-standard-card-tabs-select");
    expect(card).toHaveTextContent("Tabs select");
    expect(within(card).getAllByRole("tablist")).toHaveLength(8);

    const pillXs = within(card).getByTestId("ui-standard-tabs-select-pill-single-option-XS");
    await user.click(pillXs);
    expect(pillXs).toHaveAttribute("aria-checked", "true");
    expect(within(card).getByTestId("ui-standard-tabs-select-pill-single-option-XXL")).toHaveAttribute("aria-checked", "false");
    const tileDelivery = within(card).getByTestId("ui-standard-tabs-select-tile-single-option-PayOnDelivery");
    await user.click(tileDelivery);
    expect(tileDelivery).toHaveAttribute("aria-checked", "true");
    const multiM = within(card).getByTestId("ui-standard-tabs-select-pill-multi-option-M");
    await user.click(multiM);
    expect(multiM).toHaveAttribute("aria-pressed", "true");
    expect(within(card).getByTestId("ui-standard-tabs-select-pill-multi-option-S")).toHaveAttribute("aria-pressed", "true");
    await user.click(within(card).getByTestId("ui-standard-tabs-select-filter-inactive"));
    expect(within(card).getByTestId("ui-standard-tabs-select-filter-inactive")).toHaveAttribute("aria-pressed", "true");
    expect(within(card).getByTestId("ui-standard-tabs-select-filter-all")).toHaveAttribute("aria-pressed", "false");

    const themeCards = within(card).getByTestId("ui-standard-tabs-select-cards");
    const system = within(themeCards).getByRole("radio", { name: "Appearance: System" });
    const dark = within(themeCards).getByRole("radio", { name: "Appearance: Dark" });
    expect(system).toHaveAttribute("aria-checked", "true");
    await user.click(dark);
    expect(dark).toHaveAttribute("aria-checked", "true");
    expect(system).toHaveAttribute("aria-checked", "false");
    expect(within(card).getByRole("radiogroup", { name: "Density" })).toBeInTheDocument();

    const basicTabs = within(card).getByTestId("ui-standard-card-basic-tabs");
    expect(within(basicTabs).getByTestId("ui-standard-basic-tabs-title")).toHaveTextContent("Basic tabs");
    expect(within(basicTabs).getByTestId("ui-standard-basic-tabs-copy-command")).toHaveAttribute(
      "data-command",
      "BASIC TABS + ICON AND LABEL",
    );
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-basic-command")).toHaveTextContent(
      "cursor: ext-tabs-basic-icon-and-label",
    );
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-inline-command")).toHaveTextContent(
      "cursor: ext-tabs-basic-inline-label",
    );
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-no-caps-command")).toHaveTextContent(
      "cursor: ext-tabs-basic-no-caps",
    );
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-icons-command")).toHaveTextContent(
      "cursor: ext-tabs-basic-icons-only",
    );
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-scroll-command")).toHaveTextContent(
      "cursor: ext-tabs-basic-scrollable",
    );
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-scroll-more-command")).toHaveTextContent(
      "cursor: ext-tabs-basic-scrollable-more",
    );
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-teal-more-more")).toBeInTheDocument();
    await user.click(within(basicTabs).getByTestId("ui-standard-qtabs-teal-more-more"));
    await user.click(await screen.findByTestId("ui-standard-qtabs-teal-more-more-item-settings"));
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-value")).toHaveTextContent("Selected: settings");
    await user.click(within(basicTabs).getByTestId("ui-standard-qtabs-teal-mails"));

    const teal = within(basicTabs).getByTestId("ui-standard-qtabs-teal");
    expect(within(teal).getByRole("tab", { name: "Mails" })).toHaveAttribute("aria-selected", "true");
    await user.click(within(teal).getByRole("tab", { name: "Alarms" }));
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-value")).toHaveTextContent("Selected: alarms");
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-purple-alarms")).toHaveAttribute("aria-selected", "true");
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-yellow-alarms")).toHaveAccessibleName("alarms");

    const alarms = within(teal).getByRole("tab", { name: "Alarms" });
    expect(alarms).toHaveAttribute("tabindex", "0");
    await user.keyboard("{ArrowRight}");
    const movies = within(teal).getByRole("tab", { name: "Movies" });
    expect(movies).toHaveFocus();
    expect(movies).toHaveAttribute("aria-selected", "false");
    await user.keyboard("{ArrowRight}");
    expect(within(teal).getByRole("tab", { name: "Mails" })).toHaveFocus();
    await user.keyboard("{End}");
    expect(movies).toHaveFocus();
    await user.keyboard("{Enter}");
    expect(movies).toHaveAttribute("aria-selected", "true");
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-value")).toHaveTextContent("Selected: movies");

    expect(within(basicTabs).getByTestId("ui-standard-qtabs-primary-addressbook")).toHaveTextContent("Address Book");
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-primary-caret-left")).toBeInTheDocument();
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-primary-caret-right")).toBeInTheDocument();

    await user.click(within(basicTabs).getByTestId("ui-standard-qtabs-primary-mails"));
    expect(within(basicTabs).getByTestId("ui-standard-qtabs-value")).toHaveTextContent("Selected: mails");
    await user.click(within(basicTabs).getByTestId("ui-standard-qtabs-primary-caret-right"));
    await vi.waitFor(() => {
      expect(within(basicTabs).getByTestId("ui-standard-qtabs-value")).toHaveTextContent("Selected: alarms");
    });
    await user.dblClick(within(basicTabs).getByTestId("ui-standard-qtabs-primary-caret-right"));
    await vi.waitFor(() => {
      expect(within(basicTabs).getByTestId("ui-standard-qtabs-value")).toHaveTextContent("Selected: settings");
    });

    const tabPanels = within(card).getByTestId("ui-standard-card-tab-panels");
    expect(within(tabPanels).getByTestId("ui-standard-tab-panels-title")).toHaveTextContent("Tab panels");
    expect(within(tabPanels).getByTestId("ui-standard-qtabs-panels-top-command")).toHaveTextContent(
      "cursor: ext-tabs-panels-with-tabs",
    );
    expect(within(tabPanels).getByTestId("ui-standard-qtabs-panels-bottom-command")).toHaveTextContent(
      "cursor: ext-tabs-panels-tabs-below",
    );
    expect(within(tabPanels).getByTestId("ui-standard-qtabs-panels-panel-mails")).not.toHaveAttribute("hidden");
    await user.click(within(tabPanels).getByTestId("ui-standard-qtabs-panels-tabs-alarms"));
    expect(within(tabPanels).getByTestId("ui-standard-qtabs-panels-value")).toHaveTextContent("Panel: alarms");
    expect(within(tabPanels).getByTestId("ui-standard-qtabs-panels-panel-alarms")).not.toHaveAttribute("hidden");
    expect(within(tabPanels).getByTestId("ui-standard-qtabs-panels-panel-mails")).toHaveAttribute("hidden");
    expect(within(tabPanels).getByTestId("ui-standard-qtabs-panels-below-panel-alarms")).not.toHaveAttribute(
      "hidden",
    );
  });

  it("opens the Dialog playground and samples", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-dialog"));
    expect(screen.getByTestId("ui-standards-filter-dialog")).toHaveAttribute("data-selected", "true");
    const playground = screen.getByTestId("ui-standard-card-dialog-playground");
    const samples = screen.getByTestId("ui-standard-card-dialog");
    expect(playground).toHaveTextContent("Dialog playground");
    expect(playground.compareDocumentPosition(samples) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(screen.queryByTestId("ui-standard-card-modal")).not.toBeInTheDocument();

    const panel = within(playground).getByTestId("ui-standard-dialog-pg-panel");
    expect(panel).toHaveAttribute("data-intent", "primary");
    expect(panel).toHaveAttribute("data-size", "md");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext dialog");

    await choosePlaygroundDropdown(user, "ui-standard-dialog-pg-intent", "danger");
    await choosePlaygroundDropdown(user, "ui-standard-dialog-pg-size", "large");
    await user.click(within(playground).getByTestId("ui-standard-dialog-pg-footer"));
    expect(panel).toHaveAttribute("data-intent", "danger");
    expect(panel).toHaveAttribute("data-size", "lg");
    expect(within(playground).queryByTestId("ui-standard-dialog-pg-panel-footer")).not.toBeInTheDocument();
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext dialog danger lg no-footer",
    );

    await user.click(within(playground).getByTestId("ui-standard-dialog-pg-open"));
    expect(await screen.findByTestId("ui-standard-dialog-pg-modal")).toHaveTextContent("Save changes?");
    await user.click(screen.getByTestId("ui-standard-dialog-pg-modal-close"));
    expect(screen.queryByTestId("ui-standard-dialog-pg-modal")).not.toBeInTheDocument();

    await user.click(within(playground).getByTestId("ui-standard-dialog-pg-reset"));
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext dialog");

    await user.click(within(samples).getByTestId("ui-standard-dialog-sample-delete"));
    expect(await screen.findByTestId("ui-standard-dialog-delete")).toHaveTextContent("Delete this record?");
    await user.keyboard("{Escape}");
    expect(screen.queryByTestId("ui-standard-dialog-delete")).not.toBeInTheDocument();

    await user.click(within(samples).getByTestId("ui-standard-dialog-sample-persistent"));
    expect(await screen.findByTestId("ui-standard-dialog-persistent")).toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(screen.getByTestId("ui-standard-dialog-persistent")).toBeInTheDocument();
  });

  it("shows Quasar Basic and Styling dialog examples", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-dialog"));
    const samples = screen.getByTestId("ui-standard-card-dialog");
    expect(within(samples).getByTestId("ui-standard-qdialog-basic")).toHaveTextContent("Basic");
    expect(within(samples).getByTestId("ui-standard-qdialog-styling")).toHaveTextContent("Styling");

    await user.click(within(samples).getByTestId("ui-standard-qdialog-open-alert"));
    expect(await screen.findByRole("dialog", { name: "Alert" })).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-qdialog-alert-ok"));
    expect(screen.queryByTestId("ui-standard-qdialog-alert")).not.toBeInTheDocument();

    await user.click(within(samples).getByTestId("ui-standard-qdialog-open-confirm"));
    expect(await screen.findByTestId("ui-standard-qdialog-confirm")).toHaveTextContent(
      "You are currently not connected to any network.",
    );
    await user.keyboard("{Escape}");
    await user.click(screen.getByTestId("ui-standard-qdialog-confirm-backdrop"));
    expect(screen.getByTestId("ui-standard-qdialog-confirm")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-qdialog-confirm-cancel"));
    expect(screen.queryByTestId("ui-standard-qdialog-confirm")).not.toBeInTheDocument();

    await user.click(within(samples).getByTestId("ui-standard-qdialog-open-icon"));
    expect(await screen.findByRole("dialog", { name: "Close icon" })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(screen.queryByTestId("ui-standard-qdialog-icon")).not.toBeInTheDocument();

    await user.click(within(samples).getByTestId("ui-standard-qdialog-open-bar2"));
    expect(await screen.findByTestId("ui-standard-qdialog-bar2")).toHaveClass("ui-standards-qdialog__card--primary");
    await user.click(screen.getByTestId("ui-standard-qdialog-bar2-close"));
    expect(screen.queryByTestId("ui-standard-qdialog-bar2")).not.toBeInTheDocument();

    await user.click(within(samples).getByTestId("ui-standard-qdialog-open-toolbar"));
    expect(await screen.findByTestId("ui-standard-qdialog-toolbar")).toHaveTextContent("Quasar Framework");
  });

  it("opens the Table playground", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-tables"));
    expect(screen.getByTestId("ui-standard-card-table-playground")).toHaveTextContent("Table playground");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-separator", "horizontal");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveTextContent("Frozen Yogurt");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveClass("ui-standards-qtable--bordered");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-bordered", "true");
    expect(screen.queryByTestId("ui-standard-table-pg-bordered")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext table");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-elevation", "3");
    const elevationSlider = screen.getByTestId("ui-standard-table-pg-elevation");
    elevationSlider.focus();
    await user.keyboard("{Home}");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-elevation", "1");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveStyle({ "--qtable-shadow": "none" });
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext table flat");
    await user.keyboard("{End}");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-elevation", "5");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext table raised-5");
    await user.keyboard("{ArrowLeft}{ArrowLeft}");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-elevation", "3");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext table");
    expect(screen.queryByTestId("ui-standard-card-table")).not.toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-separator", "cell");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-separator", "cell");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveClass("ui-standards-qtable--cell");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-density", "balance");
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-density", "compact");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-density", "compact");
    expect(screen.getByTestId("ui-standard-table-pg-tab-table")).toHaveAttribute("aria-selected", "true");
    expect(screen.getByTestId("ui-standard-table-pg-table-card")).toHaveTextContent("Table");
    await user.click(screen.getByTestId("ui-standard-table-pg-tab-header"));
    expect(screen.getByTestId("ui-standard-table-pg-header-card")).toHaveTextContent("Intent");
    expect(screen.getByTestId("ui-standard-table-pg-header-card")).not.toHaveTextContent("Treats");
    await user.click(screen.getByTestId("ui-standard-table-pg-tab-footer"));
    expect(screen.getByTestId("ui-standard-table-pg-footer-card")).toHaveTextContent("Footer");
    expect(screen.getByTestId("ui-standard-table-pg-footer-card")).toHaveTextContent("Intent");
    expect(screen.getByTestId("ui-standard-table-pg-footer-card")).toHaveTextContent("Appearance");
    expect(screen.getByTestId("ui-standard-table-pg-footer")).toHaveTextContent("1-10 of 110");
    await user.click(screen.getByTestId("ui-standard-table-pg-tab-features"));
    expect(screen.getByTestId("ui-standard-table-pg-features-card")).toHaveTextContent("Search");
    expect(screen.getByTestId("ui-standard-table-pg-features-card")).toHaveTextContent("Export");
    expect(screen.getByTestId("ui-standard-table-pg-features-card")).toHaveTextContent("Filter");
    expect(screen.queryByTestId("ui-standard-table-pg-flat")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-table-pg-tab-table"));
    expect(screen.queryByTestId("ui-standard-table-pg-square")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-table-pg-dark")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-table-pg-loading"));
    await user.click(screen.getByTestId("ui-standard-table-pg-tab-features"));
    await user.click(screen.getByTestId("ui-standard-table-pg-search-toggle"));
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-export", "pdf");
    await user.click(screen.getByTestId("ui-standard-table-pg-filter-toggle"));
    await user.click(screen.getByTestId("ui-standard-table-pg-tab-table"));
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-search", "true");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-filter", "true");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-export", "pdf");
    expect(screen.getByTestId("ui-standard-table-pg-search")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-table-pg-filter")).toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-filter", "eclair");
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-filter", "cupcake");
    expect(screen.getByTestId("ui-standard-table-pg-row-eclair")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-table-pg-row-cupcake")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-table-pg-row-frozen-yogurt")).not.toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-filter", "eclair");
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-filter", "cupcake");
    expect(screen.getByTestId("ui-standard-table-pg-row-frozen-yogurt")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-table-pg-export-pdf")).toHaveTextContent("PDF");
    await user.type(screen.getByTestId("ui-standard-table-pg-search"), "Cupcake");
    expect(screen.getByTestId("ui-standard-table-pg-row-cupcake")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-table-pg-row-frozen-yogurt")).not.toBeInTheDocument();
    await user.clear(screen.getByTestId("ui-standard-table-pg-search"));
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table separator-cell compact loading search export-pdf filter",
    );
    await user.click(screen.getByTestId("ui-standard-table-pg-tab-features"));
    await user.click(screen.getByTestId("ui-standard-table-pg-search-toggle"));
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-export", "pdf");
    await user.click(screen.getByTestId("ui-standard-table-pg-filter-toggle"));
    await user.click(screen.getByTestId("ui-standard-table-pg-tab-table"));
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-density", "compact");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-loading", "true");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveTextContent("Loading...");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table separator-cell compact loading",
    );
    expect(screen.queryByTestId("ui-standard-table-pg-select-all")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-table-pg-multi-select"));
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-multi-select", "true");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table separator-cell compact loading multi-select",
    );
    await user.click(screen.getByTestId("ui-standard-table-pg-select-all"));
    expect(screen.getByTestId("ui-standard-table-pg-select-frozen-yogurt")).toBeChecked();
    expect(screen.getByTestId("ui-standard-table-pg-select-cupcake")).toBeChecked();
    await user.click(screen.getByTestId("ui-standard-table-pg-select-frozen-yogurt"));
    expect(screen.getByTestId("ui-standard-table-pg-select-all")).toBePartiallyChecked();
    await user.click(screen.getByTestId("ui-standard-table-pg-multi-select"));
    expect(screen.queryByTestId("ui-standard-table-pg-select-all")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-action", "none");
    expect(screen.queryByTestId("ui-standard-table-pg-actions-frozen-yogurt")).not.toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-action", "edit");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-action", "edit");
    expect(screen.getByTestId("ui-standard-table-pg-actions-frozen-yogurt")).toBeInTheDocument();
    const editAction = screen.getByRole("button", { name: "Edit Frozen Yogurt" });
    expect(editAction).toHaveAttribute("data-shape", "round");
    expect(editAction.querySelector(".material-symbols-outlined")).toHaveTextContent("edit");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-edit-kind", "inline");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-edit-effective", "inline");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute(
      "data-editable",
      "dessert,calories,fat,carbs,protein",
    );
    await user.click(editAction);
    expect(screen.getByTestId("ui-standard-table-pg-cell-frozen-yogurt-dessert")).toHaveFocus();
    await user.click(screen.getByTestId("ui-standard-table-pg-edit-reset-frozen-yogurt"));
    expect(screen.queryByTestId("ui-standard-table-pg-cell-frozen-yogurt-dessert")).not.toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-editable", "calories");
    await user.click(screen.getByRole("button", { name: "Edit Frozen Yogurt" }));
    expect(screen.getByTestId("ui-standard-table-pg-cell-frozen-yogurt-dessert")).toHaveFocus();
    expect(screen.queryByTestId("ui-standard-table-pg-cell-frozen-yogurt-calories")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-table-pg-edit-reset-frozen-yogurt"));
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-editable", "calories");
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-edit-kind", "dialog");
    await user.click(screen.getByRole("button", { name: "Edit Frozen Yogurt" }));
    expect(screen.getByTestId("ui-standard-table-pg-edit-dialog")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-table-pg-cell-frozen-yogurt-dessert")).toHaveFocus();
    await user.click(screen.getByTestId("ui-standard-table-pg-edit-cancel"));
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-edit-kind", "inline");
    await user.click(
      within(screen.getByTestId("ui-standard-table-pg-preview-size")).getByRole("radio", {
        name: "Preview size: Mobile",
      }),
    );
    await user.click(screen.getByRole("button", { name: "Edit Frozen Yogurt" }));
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-edit-effective", "dialog");
    expect(screen.getByTestId("ui-standard-table-pg-edit-dialog")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-table-pg-edit-cancel"));
    await user.click(
      within(screen.getByTestId("ui-standard-table-pg-preview-size")).getByRole("radio", {
        name: "Preview size: Desktop",
      }),
    );
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table separator-cell compact loading action-edit",
    );
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-icon-shape", "square");
    expect(screen.getByRole("button", { name: "Edit Frozen Yogurt" })).toHaveAttribute("data-shape", "square");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table separator-cell compact loading action-edit icon-square",
    );
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-action", "edit");
    expect(screen.queryByTestId("ui-standard-table-pg-actions-frozen-yogurt")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-intent", "standard");
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-intent", "primary");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-intent", "primary");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveStyle({
      "--qtable-bg": "var(--exits-primary)",
      "--qtable-ink": "#ffffff",
    });
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table primary separator-cell compact loading",
    );
    await user.click(screen.getByTestId("ui-standard-table-pg-alternate"));
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-alternate", "true");
    expect(screen.getByTestId("ui-standard-table-pg-row-frozen-yogurt")).toHaveAttribute("data-tone", "solid");
    expect(screen.getByTestId("ui-standard-table-pg-row-ice-cream-sandwich")).toHaveAttribute("data-tone", "soft");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table primary alternate separator-cell compact loading",
    );
    await user.click(screen.getByTestId("ui-standard-table-pg-alternate"));
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-intent", "standard");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table separator-cell compact loading",
    );
    await user.click(screen.getByTestId("ui-standard-table-pg-tab-header"));
    expect(screen.getByTestId("ui-standard-table-pg-header")).toHaveAttribute("data-appearance", "standard");
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-header-intent", "primary");
    await choosePlaygroundDropdown(user, "ui-standard-table-pg-header-appearance", "soft-outline");
    expect(screen.getByTestId("ui-standard-table-pg-header")).toHaveAttribute("data-intent", "primary");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveStyle({ "--qheader-accent": "var(--exits-primary)" });
    expect(screen.getByTestId("ui-standard-table-pg-header")).toHaveAttribute("data-appearance", "soft-outline");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table separator-cell compact loading header-primary header-soft-outline",
    );
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-sort", "false");
    await user.click(screen.getByTestId("ui-standard-table-pg-sort"));
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-sort", "true");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext table separator-cell compact loading header-primary header-soft-outline sort",
    );
    await user.click(screen.getByTestId("ui-standard-table-pg-sort-dessert"));
    expect(screen.getByTestId("ui-standard-table-pg-sample").querySelector("tbody tr")).toHaveTextContent("Baklava");
    expect(screen.getByTestId("ui-standard-table-pg-frame")).toHaveAttribute("data-preview-device", "desktop");
    expect(screen.getByTestId("ui-standard-table-pg-frame")).toHaveAttribute("data-preview-width", "1280");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-layout", "table");
    expect(screen.getByTestId("ui-standard-table-pg-mode")).toHaveTextContent("Desktop · 1280px · TABLE");
    await user.click(
      within(screen.getByTestId("ui-standard-table-pg-preview-size")).getByRole("radio", {
        name: "Preview size: Tablet",
      }),
    );
    expect(screen.getByTestId("ui-standard-table-pg-frame")).toHaveAttribute("data-preview-device", "tablet");
    expect(screen.getByTestId("ui-standard-table-pg-frame")).toHaveAttribute("data-preview-width", "834");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-layout", "table");
    expect(screen.getByTestId("ui-standard-table-pg-mode")).toHaveTextContent("Tablet · 834px · TABLE");
    await user.click(
      within(screen.getByTestId("ui-standard-table-pg-preview-size")).getByRole("radio", {
        name: "Preview size: Mobile",
      }),
    );
    expect(screen.getByTestId("ui-standard-table-pg-frame")).toHaveAttribute("data-preview-device", "mobile");
    expect(screen.getByTestId("ui-standard-table-pg-frame")).toHaveAttribute("data-preview-width", "390");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-layout", "list");
    expect(screen.getByTestId("ui-standard-table-pg-mode")).toHaveTextContent("Mobile · 390px · LIST");
    expect(screen.getByTestId("ui-standard-table-pg-sample").querySelector("table")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveTextContent("Baklava");
    expect(screen.getByTestId("ui-standard-table-pg-list-sort")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-table-pg-sort-calories"));
    expect(screen.getByTestId("ui-standard-table-pg-list-sort")).toHaveTextContent("Calories");
    expect(screen.getByTestId("ui-standard-table-pg-sample").querySelector(".ui-standards-qtable-list__title")).toHaveTextContent(
      "Pudding",
    );
    await user.click(screen.getByTestId("ui-standard-table-pg-reset"));
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-separator", "horizontal");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-density", "balance");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-elevation", "3");
    expect(screen.getByTestId("ui-standard-table-pg-frame")).toHaveAttribute("data-preview-device", "desktop");
    expect(screen.getByTestId("ui-standard-table-pg-sample")).toHaveAttribute("data-layout", "table");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext table");
  }, 20_000);

  it("filters live cards by category", () => {
    renderPage("/ui-standards?category=overlays");
    expect(screen.getByTestId("ui-standard-card-drawer")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-modal")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-confirm")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-buttons")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-catalog-table")).not.toBeInTheDocument();

    cleanup();
    renderPage("/ui-standards?category=dropdown");
    expect(screen.getByTestId("ui-standard-card-dropdown-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-dropdown")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-select-api")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-selects")).not.toBeInTheDocument();
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

  it("opens Button stepper filter with the QuantityStepper variant card", () => {
    renderPage("/ui-standards?category=button-stepper");
    expect(screen.getByTestId("ui-standards-filter-button-stepper")).toHaveAttribute("data-selected", "true");
    expect(screen.queryByTestId("ui-standards-filter-forms")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-forms")).not.toBeInTheDocument();
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

  it("opens Typography from the sidenav", async () => {
    const user = userEvent.setup();
    renderPage();
    const item = screen.getByTestId("ui-standards-filter-typography");
    expect(item).toHaveTextContent("Typography");
    expect(item).not.toHaveAttribute("aria-expanded");
    expect(screen.queryByTestId("ui-standards-filter-typography-playground")).not.toBeInTheDocument();

    await user.click(item);
    expect(item).toHaveAttribute("data-selected", "true");
    const card = screen.getByTestId("ui-standard-card-typography-playground");
    expect(within(screen.getByTestId("ui-standards-live-section")).getByTestId("ui-standard-card-typography-playground")).toBe(card);
    expect(card).toHaveTextContent("Typography");
    const preview = within(card).getByTestId("ui-standards-sidenav-type-preview");
    expect(preview).toHaveClass("ui-standards-type-sample");
    expect(preview.parentElement).toHaveClass("ui-standards-type-stage");
    expect(screen.getByTestId("ui-standards-sidenav-type-weight")).toHaveTextContent("weight: 500");
    expect(screen.getByTestId("ui-standards-sidenav-type-size")).toHaveTextContent("size: 0.875rem");
    expect(preview).toHaveAttribute("data-font-weight", "500");
    expect(preview).toHaveStyle({ "--sidenav-font-weight": "500" });
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext typography");

    await user.click(screen.getByTestId("ui-standards-sidenav-type-fw-up"));
    expect(screen.getByTestId("ui-standards-sidenav-type-weight")).toHaveTextContent("weight: 510");
    expect(preview).toHaveAttribute("data-font-weight", "510");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext typography FW+1");

    await user.click(screen.getByTestId("ui-standards-sidenav-type-fz-up"));
    expect(screen.getByTestId("ui-standards-sidenav-type-size")).toHaveTextContent("size: 0.88375rem");
    expect(preview).toHaveAttribute("data-font-size", "0.88375rem");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext typography FW+1 FZ+1");

    await user.click(screen.getByTestId("ui-standards-sidenav-type-reset"));
    expect(screen.getByTestId("ui-standards-sidenav-type-weight")).toHaveTextContent("weight: 500");
    expect(screen.getByTestId("ui-standards-sidenav-type-size")).toHaveTextContent("size: 0.875rem");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext typography");

    const amount = screen.getByTestId("ui-standards-sidenav-type-amount");
    await user.clear(amount);
    await user.type(amount, "13");
    await user.click(screen.getByTestId("ui-standards-sidenav-type-fw-up"));
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext typography FW+13");
    expect(screen.getByTestId("ui-standards-sidenav-type-weight")).toHaveTextContent("weight: 565");
    await user.click(screen.getByTestId("ui-standards-sidenav-type-fw-up"));
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext typography FW+26");
    expect(screen.getByTestId("ui-standards-sidenav-type-weight")).toHaveTextContent("weight: 630");

    await user.clear(amount);
    await user.type(amount, "10");
    await user.click(screen.getByTestId("ui-standards-sidenav-type-fz-up"));
    await user.click(screen.getByTestId("ui-standards-sidenav-type-fz-up"));
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext typography FW+26 FZ+20");
    expect(screen.getByTestId("ui-standards-sidenav-type-size")).toHaveTextContent("size: 1.05rem");
    expect(preview).toHaveAttribute("data-font-weight", "630");
    expect(preview).toHaveAttribute("data-font-size", "1.05rem");

    await user.click(screen.getByTestId("ui-standards-sidenav-type-reset"));
    await user.clear(amount);
    await user.type(amount, "13");
    await user.click(screen.getByTestId("ui-standards-sidenav-type-fwz-up"));
    await user.click(screen.getByTestId("ui-standards-sidenav-type-fwz-up"));
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext typography FWZ+26");
    expect(screen.getByTestId("ui-standards-sidenav-type-weight")).toHaveTextContent("weight: 630");
    expect(screen.getByTestId("ui-standards-sidenav-type-size")).toHaveTextContent("size: 1.1025rem");
    expect(card).toHaveClass("w-1/2");
  });

  it("opens Lucide inside the page and links Google icons in a new tab", async () => {
    const user = userEvent.setup();
    renderPage();
    const group = screen.getByTestId("ui-standards-filter-icons");
    expect(group).toHaveTextContent("Icons");
    expect(group).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByTestId("ui-standards-filter-lucide")).toHaveTextContent("Lucide");
    const google = screen.getByTestId("ui-standards-filter-google-icons");
    expect(google).toHaveAttribute("href", "https://fonts.google.com/icons");
    expect(google).toHaveAttribute("target", "_blank");
    await user.click(screen.getByTestId("ui-standards-filter-lucide"));
    expect(screen.getByTestId("ui-standards-lucide-frame")).toHaveAttribute("src", "https://lucide.dev/icons");
    expect(screen.getByRole("link", { name: "Open in a new tab" })).toHaveAttribute("href", "https://lucide.dev/icons");
  });

  it("nests Input, Radio, and Checkbox under Form Components", async () => {
    const user = userEvent.setup();
    renderPage();
    const group = screen.getByTestId("ui-standards-filter-form-components");
    expect(group).toHaveTextContent("Form Components");
    expect(group).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByTestId("ui-standards-filter-inputs")).toHaveTextContent("Input");
    expect(screen.getByTestId("ui-standards-filter-radio")).toHaveTextContent("Radio");
    expect(screen.getByTestId("ui-standards-filter-checkbox")).toHaveTextContent("Checkbox");
    expect(screen.getByTestId("ui-standards-filter-toggle")).toHaveTextContent("Toggle");
    const selects = screen.getByTestId("ui-standards-filter-select-components");
    expect(selects).toHaveTextContent("Selects");
    expect(selects).toHaveAttribute("aria-expanded", "true");
    expect(screen.getByTestId("ui-standards-filter-dropdown")).toHaveTextContent("Select");
    expect(screen.getByTestId("ui-standards-filter-tabs-select")).toHaveTextContent("Tabs select");
    expect(screen.queryByTestId("ui-standards-filter-selects")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standards-filter-filter-select")).not.toBeInTheDocument();
    const formGroup = screen.getByRole("group", { name: "Form Components" });
    const selectsGroup = screen.getByRole("group", { name: "Selects" });
    expect(formGroup).toContainElement(selectsGroup);
    expect(selectsGroup).toContainElement(screen.getByTestId("ui-standards-filter-dropdown"));
    expect(selectsGroup).toContainElement(screen.getByTestId("ui-standards-filter-tabs-select"));
    await user.click(selects);
    expect(selects).toHaveAttribute("aria-expanded", "false");
    expect(screen.queryByTestId("ui-standards-filter-tabs-select")).not.toBeInTheDocument();
    await user.click(selects);
    expect(screen.getByTestId("ui-standards-filter-tabs-select")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-filter-slider")).toHaveTextContent("Slider");
    expect(screen.getByTestId("ui-standards-filter-range")).toHaveTextContent("Range");
    expect(screen.getByTestId("ui-standards-filter-date")).toHaveTextContent("Date picker");
    expect(screen.getByTestId("ui-standards-filter-time")).toHaveTextContent("Time picker");
    await user.click(screen.getByTestId("ui-standards-filter-radio"));
    expect(screen.getByTestId("ui-standards-filter-radio")).toHaveAttribute("data-selected", "true");
    const playground = screen.getByTestId("ui-standard-card-radio-playground");
    const samples = screen.getByTestId("ui-standard-card-radio");
    expect(playground.compareDocumentPosition(samples) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(samples).getByTestId("ui-standard-radio-standard")).toHaveStyle({ "--qradio-color": "#9e9e9e" });
    expect(within(samples).getByTestId("ui-standard-radio-standard")).not.toHaveClass("ui-standards-qradio--dark-ink");
    expect(within(samples).getByTestId("ui-standard-radio-icon-standard")).toHaveStyle({ "--qradio-color": "#9e9e9e" });
    expect(within(playground).getByTestId("ui-standard-radio-pg-field")).toHaveStyle({ "--qradio-color": "#9e9e9e" });
    expect(within(samples).getByTestId("ui-standard-radio-primary")).toHaveAttribute("aria-checked", "true");
    await user.click(within(samples).getByTestId("ui-standard-radio-primary"));
    expect(within(samples).getByTestId("ui-standard-radio-primary")).toHaveAttribute("aria-checked", "false");
    await user.click(within(samples).getByTestId("ui-standard-radio-icon-primary"));
    expect(within(samples).getByTestId("ui-standard-radio-icon-primary")).toHaveAttribute("aria-checked", "false");
    await user.click(within(samples).getByTestId("ui-standard-radio-secondary"));
    expect(within(samples).getByTestId("ui-standard-radio-secondary")).toHaveAttribute("aria-checked", "false");
    expect(within(samples).getByTestId("ui-standard-radio-primary")).toHaveStyle({ "--qradio-color": "#1976d2" });
    expect(within(samples).queryByText("Force dark mode")).not.toBeInTheDocument();
    expect(within(samples).getByTestId("ui-standard-radio-icons")).toBeInTheDocument();
    expect(within(samples).getByTestId("ui-standard-radio-icon-primary")).toHaveClass("ui-standards-qradio--icons");
    expect(within(samples).getByTestId("ui-standard-radio-danger")).toBeInTheDocument();
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext radio");
    await choosePlaygroundDropdown(user, "ui-standard-radio-pg-intent", "primary");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext radio primary",
    );
    await choosePlaygroundDropdown(user, "ui-standard-radio-pg-type", "with-icons");
    await choosePlaygroundDropdown(user, "ui-standard-radio-pg-density", "comfort");
    expect(within(playground).getByTestId("ui-standard-radio-pg-field")).toHaveClass("ui-standards-qradio--icons");
    expect(within(playground).getByTestId("ui-standard-radio-pg-preview")).toHaveAttribute("data-density", "comfort");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext radio primary icons comfort",
    );
    expect(screen.queryByTestId("ui-standard-card-inputs")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standards-filter-checkbox"));
    expect(screen.getByTestId("ui-standards-filter-checkbox")).toHaveAttribute("data-selected", "true");
    const checkboxPlayground = screen.getByTestId("ui-standard-card-checkbox-playground");
    const checkboxSamples = screen.getByTestId("ui-standard-card-checkbox");
    expect(checkboxPlayground.compareDocumentPosition(checkboxSamples) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-standard")).toHaveStyle({
      "--qcheckbox-color": "#9e9e9e",
    });
    await user.click(within(checkboxSamples).getByTestId("ui-standard-checkbox-standard"));
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-standard")).toHaveAttribute("aria-checked", "false");
    await user.click(within(checkboxSamples).getByTestId("ui-standard-checkbox-primary"));
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-primary")).toHaveAttribute("aria-checked", "false");
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-standard")).not.toHaveClass(
      "ui-standards-qcheckbox--dark-ink",
    );
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-primary")).toHaveStyle({
      "--qcheckbox-color": "#1976d2",
    });
    expect(within(checkboxSamples).queryByText("Force dark mode")).not.toBeInTheDocument();
    expect(within(checkboxSamples).queryByTestId("ui-standard-checkbox-icons")).not.toBeInTheDocument();
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-toggle-tf")).toHaveAttribute("aria-checked", "mixed");
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-toggle-ft")).toHaveAttribute("aria-checked", "false");
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-toggle-tf-indet")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-toggle-ft-indet")).toHaveAttribute(
      "aria-checked",
      "false",
    );
    expect(within(checkboxPlayground).getByTestId("ui-standard-checkbox-pg-field")).toHaveStyle({
      "--qcheckbox-color": "#9e9e9e",
    });
    expect(within(checkboxPlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext checkbox comfort",
    );
    expect(within(checkboxPlayground).queryByTestId("ui-standard-checkbox-pg-type")).not.toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-checkbox-pg-intent", "primary");
    await choosePlaygroundDropdown(user, "ui-standard-checkbox-pg-density", "comfort");
    expect(within(checkboxPlayground).getByTestId("ui-standard-checkbox-pg-preview")).toHaveAttribute(
      "data-density",
      "comfort",
    );
    expect(within(checkboxPlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext checkbox primary comfort",
    );
    await user.click(within(checkboxSamples).getByTestId("ui-standard-checkbox-toggle-tf"));
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-toggle-tf")).toHaveAttribute("aria-checked", "true");
    await user.click(within(checkboxSamples).getByTestId("ui-standard-checkbox-toggle-reset"));
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-toggle-tf")).toHaveAttribute("aria-checked", "mixed");
    expect(within(checkboxSamples).getByTestId("ui-standard-checkbox-toggle-tf-indet")).toHaveAttribute(
      "aria-checked",
      "mixed",
    );
    expect(screen.queryByTestId("ui-standard-card-radio")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standards-filter-toggle"));
    expect(screen.getByTestId("ui-standards-filter-toggle")).toHaveAttribute("data-selected", "true");
    const togglePlayground = screen.getByTestId("ui-standard-card-toggle-playground");
    const toggleSamples = screen.getByTestId("ui-standard-card-toggle");
    expect(togglePlayground.compareDocumentPosition(toggleSamples) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-standard")).toHaveStyle({ "--qtoggle-color": "#9e9e9e" });
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-standard")).toHaveClass("ui-standards-qtoggle--standard");
    await user.click(within(toggleSamples).getByTestId("ui-standard-toggle-standard"));
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-standard")).toHaveAttribute("data-state", "off");
    await user.click(within(toggleSamples).getByTestId("ui-standard-toggle-icon-primary"));
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-icon-primary")).toHaveAttribute("data-state", "off");
    await user.click(within(toggleSamples).getByTestId("ui-standard-toggle-primary"));
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-primary")).toHaveAttribute("data-state", "off");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-icon-primary")).toHaveClass("ui-standards-qtoggle--icons");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pill-standard")).toHaveClass("ui-standards-pill-switch--standard");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-primary")).toHaveStyle({ "--qtoggle-color": "#1976d2" });
    expect(within(toggleSamples).queryByText("Force dark mode")).not.toBeInTheDocument();
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-colors")).toHaveTextContent("Standard");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-icons")).toHaveTextContent("Standard with icons");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pill")).toHaveTextContent("Solid");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pill-outline-primary")).toHaveClass(
      "ui-standards-pill-switch--outline",
    );
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pill-black")).toHaveAttribute("data-state", "on");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pill-icon-black")).toBeInTheDocument();
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pill-label-success")).toHaveTextContent("ON");
    await user.click(within(toggleSamples).getByTestId("ui-standard-toggle-pill-label-success"));
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pill-label-success")).toHaveAttribute("data-state", "off");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pill-label-success")).toHaveTextContent("OFF");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-order-tf")).toHaveAttribute("data-state", "indet");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-order-tf-indet")).toHaveAttribute("data-state", "on");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pilot-badge")).toHaveTextContent(
      "TOGGLE STANDARD — APPROVED / LOCKED",
    );
    expect(within(togglePlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext toggle comfort",
    );
    await user.click(within(togglePlayground).getByTestId("ui-standards-copy-command-expand"));
    expect(within(togglePlayground).getByText(/Use the locked ExItS Toggle Standard/)).toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-toggle-pg-intent", "primary");
    await choosePlaygroundDropdown(user, "ui-standard-toggle-pg-density", "comfort");
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-preview")).toHaveAttribute("data-density", "comfort");
    expect(within(togglePlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext toggle primary comfort",
    );
    expect(within(togglePlayground).queryByTestId("ui-standard-toggle-pg-dense")).not.toBeInTheDocument();
    expect(within(togglePlayground).queryByRole("option", { name: "Ghost" })).not.toBeInTheDocument();
    expect(within(togglePlayground).queryByRole("option", { name: "Rounded" })).not.toBeInTheDocument();
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-soft-primary")).toHaveClass(
      "ui-standards-qtoggle--appearance-soft",
    );
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-soft-outline-primary")).toHaveClass(
      "ui-standards-qtoggle--appearance-soft-outline",
    );
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-pill-soft-outline-primary")).toHaveClass(
      "ui-standards-pill-switch--appearance-soft-outline",
    );
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-reset")).toHaveAttribute("aria-label", "Reset");
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-theme")).toBeInTheDocument();
    await user.click(within(togglePlayground).getByTestId("ui-standard-toggle-pg-field"));
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-field")).toHaveAttribute("data-state", "off");
    await user.click(within(togglePlayground).getByTestId("ui-standard-toggle-pg-field"));
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-field")).toHaveAttribute("data-state", "on");
    await choosePlaygroundDropdown(user, "ui-standard-toggle-pg-variety", "solid");
    await choosePlaygroundDropdown(user, "ui-standard-toggle-pg-appearance", "raised");
    await choosePlaygroundDropdown(user, "ui-standard-toggle-pg-icon", "check");
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-preview")).toHaveAttribute("data-variety", "solid");
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-preview")).toHaveAttribute("data-appearance", "raised");
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-field")).toHaveClass(
      "ui-standards-pill-switch--appearance-raised",
    );
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-field-icon")).toBeInTheDocument();
    expect(within(togglePlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext toggle primary solid raised comfort check",
    );
    await user.click(within(togglePlayground).getByTestId("ui-standard-toggle-pg-alternate"));
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-preview")).toHaveAttribute("data-alternate", "true");
    expect(within(togglePlayground).getByTestId("ui-standard-toggle-pg-field-alternate")).toHaveClass(
      "ui-standards-pill-switch--appearance-soft",
    );
    expect(within(togglePlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext toggle primary solid raised alternate comfort check",
    );
    await user.click(within(togglePlayground).getByTestId("ui-standard-toggle-pg-alternate"));
    await user.click(within(toggleSamples).getByTestId("ui-standard-toggle-order-tf"));
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-order-tf")).toHaveAttribute("data-state", "on");
    await user.click(within(toggleSamples).getByTestId("ui-standard-toggle-order-reset"));
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-order-tf")).toHaveAttribute("data-state", "indet");
    expect(within(toggleSamples).getByTestId("ui-standard-toggle-order-tf-indet")).toHaveAttribute("data-state", "on");
    expect(screen.queryByTestId("ui-standard-card-checkbox")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standards-filter-dropdown"));
    expect(screen.getByTestId("ui-standards-filter-dropdown")).toHaveAttribute("data-selected", "true");
    const dropdownPlayground = screen.getByTestId("ui-standard-card-dropdown-playground");
    const dropdownSamples = screen.getByTestId("ui-standard-card-dropdown");
    expect(dropdownPlayground.compareDocumentPosition(dropdownSamples) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(dropdownSamples).getByTestId("ui-standard-dropdown-standard")).toHaveStyle({ "--qdropdown-color": "#ffffff" });
    expect(within(dropdownSamples).getByTestId("ui-standard-dropdown-standard")).toHaveClass("ui-standards-qdropdown--standard");
    expect(within(dropdownSamples).getByTestId("ui-standard-dropdown-pilot-badge")).toHaveTextContent(
      "DROPDOWN STANDARD — APPROVED / LOCKED",
    );
    expect(within(dropdownSamples).getByTestId("ui-standard-dropdown-filled-primary")).toHaveClass("ui-standards-qdropdown--filled");
    expect(within(dropdownSamples).getByTestId("ui-standard-dropdown-soft-primary")).toHaveClass("ui-standards-qdropdown--soft");
    expect(within(dropdownSamples).getByTestId("ui-standard-dropdown-soft-outline-primary")).toHaveClass(
      "ui-standards-qdropdown--soft-outline",
    );
    expect(within(dropdownSamples).getByTestId("ui-standard-dropdown-outlined-primary")).toHaveClass("ui-standards-qdropdown--outlined");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-reset")).toHaveAttribute("aria-label", "Reset");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-theme")).toBeInTheDocument();
    await user.click(within(dropdownSamples).getByTestId("ui-standard-dropdown-standard-field"));
    await user.click(within(dropdownSamples).getByTestId("ui-standard-dropdown-standard-option-option-2"));
    expect(within(dropdownSamples).getByTestId("ui-standard-dropdown-standard-field")).toHaveTextContent("Option 2");
    expect(within(dropdownPlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext dropdown primary outlined comfort",
    );
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field").querySelector(".ui-standards-qdropdown__value")?.textContent).toBe("");
    const intentSelect = await openPlaygroundDropdown(user, "ui-standard-dropdown-pg-intent");
    expect(within(intentSelect).getByTestId("ui-standard-dropdown-pg-intent-option-primary")).toHaveStyle({
      "--qoption-color": "var(--exits-primary)",
    });
    await user.click(within(intentSelect).getByTestId("ui-standard-dropdown-pg-intent-option-primary"));
    await choosePlaygroundDropdown(user, "ui-standard-dropdown-pg-appearance", "outlined");
    await choosePlaygroundDropdown(user, "ui-standard-dropdown-pg-density", "comfort");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-preview")).toHaveAttribute("data-density", "comfort");
    expect(within(dropdownPlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext dropdown primary outlined comfort",
    );
    await user.click(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-default-global"));
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-default-global")).toHaveAttribute(
      "data-default-appearance",
      "outlined",
    );
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-default-global")).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    await choosePlaygroundDropdown(user, "ui-standard-dropdown-pg-shape", "rounded");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field")).toHaveAttribute("data-shape", "rounded");
    const appearanceSelect = await openPlaygroundDropdown(user, "ui-standard-dropdown-pg-appearance");
    expect(within(appearanceSelect).getByTestId("ui-standard-dropdown-pg-appearance-option-standard")).toBeDisabled();
    expect(within(dropdownPlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext dropdown primary outlined rounded comfort",
    );
    const transitionSelect = await openPlaygroundDropdown(user, "ui-standard-dropdown-pg-transition");
    expect(within(transitionSelect).getAllByRole("option").map((option) => option.textContent)).toEqual([
      "Flip up",
      "Flip down",
      "Scale",
      "Jump up",
    ]);
    await choosePlaygroundDropdown(user, "ui-standard-dropdown-pg-transition", "flip-up");
    expect(within(dropdownPlayground).queryByTestId("ui-standard-dropdown-pg-search")).not.toBeInTheDocument();
    await user.click(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-multi"));
    expect(within(dropdownPlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext dropdown primary outlined rounded comfort flip-up multi",
    );
    await user.click(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-field"));
    const dropdownMenu = within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-menu");
    expect(dropdownMenu).toHaveAttribute("data-transition", "flip-up");
    expect(dropdownMenu).toHaveAttribute("data-state", "open");
    expect(within(dropdownMenu).queryByTestId("ui-standard-dropdown-pg-field-search")).not.toBeInTheDocument();
    await user.click(within(dropdownMenu).getByTestId("ui-standard-dropdown-pg-field-select-all"));
    expect(within(dropdownMenu).getByTestId("ui-standard-dropdown-pg-field-option-option-1").querySelector(".ui-standards-qdropdown__check--on")).not.toBeNull();
    expect(within(dropdownMenu).getByText("All")).toBeInTheDocument();
    expect(within(dropdownMenu).queryByText("Select all")).not.toBeInTheDocument();
    expect(within(dropdownMenu).queryByText("Deselect all")).not.toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-field")).toHaveTextContent(
      "Option 1, Option 2, Option 3, Option 4, Option 5, Option 6",
    );
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-preview-width")).toHaveClass("w-1/2");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-preview")).toContainElement(
      within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-default-preview"),
    );
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-default-preview")).toHaveTextContent("DEFAULT GLOBAL");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-default-field").querySelector(".ui-standards-qdropdown__value")?.textContent).toBe("");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-default-preview")).toHaveAttribute("data-density", "comfort");
    await choosePlaygroundDropdown(user, "ui-standard-dropdown-pg-value", "short-value");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-field")).toHaveTextContent("6 Selected");
    await choosePlaygroundDropdown(user, "ui-standard-dropdown-pg-value", "chips-value");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-chips").querySelectorAll(".ui-standards-qdropdown__chip")).toHaveLength(6);
    await user.click(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-placeholder"));
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field")).toHaveAttribute("data-placeholder-only", "true");
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field").querySelector(".ui-standards-qdropdown__label")).toBeNull();
    expect(within(dropdownPlayground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext dropdown primary outlined rounded comfort flip-up multi placeholder value-chips",
    );
    await user.click(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-placeholder"));
    await choosePlaygroundDropdown(user, "ui-standard-dropdown-pg-value", "standard");
    await user.click(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-field"));
    const reopenedMenu = within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-menu");
    await user.click(within(reopenedMenu).getByTestId("ui-standard-dropdown-pg-field-select-all"));
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-search")).toHaveAttribute("placeholder", "");
    await user.type(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-search"), "option 2");
    expect(within(reopenedMenu).getByTestId("ui-standard-dropdown-pg-field-option-option-2")).toBeInTheDocument();
    expect(within(reopenedMenu).queryByTestId("ui-standard-dropdown-pg-field-option-option-6")).not.toBeInTheDocument();
    await user.click(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-field-search-clear"));
    expect(within(reopenedMenu).getByTestId("ui-standard-dropdown-pg-field-option-option-6")).toBeInTheDocument();
    expect(within(reopenedMenu).getByText("All")).toBeInTheDocument();
    expect(within(dropdownPlayground).queryByTestId("ui-standard-dropdown-pg-light")).not.toBeInTheDocument();
    expect(within(dropdownPlayground).queryByTestId("ui-standard-dropdown-pg-dark")).not.toBeInTheDocument();
    const themeButton = within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-theme");
    await user.click(themeButton);
    const theme = document.documentElement.getAttribute("data-theme");
    expect(theme === "light" || theme === "dark").toBe(true);
    expect(within(dropdownPlayground).getByTestId("ui-standard-dropdown-pg-preview")).toHaveAttribute(
      "data-preview-theme",
      theme,
    );
    await user.click(themeButton);
    expect(document.documentElement.getAttribute("data-theme")).toBe(theme === "dark" ? "light" : "dark");
    window.localStorage.removeItem("exits.pos-client.ui-preferences.v1");
    document.documentElement.dataset.theme = "system";
    expect(screen.queryByTestId("ui-standard-card-toggle")).not.toBeInTheDocument();
  }, 20000);

  it("opens Slider and Range under Form Components", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-slider"));
    expect(screen.getByTestId("ui-standard-card-slider-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-slider")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-slider-standard")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-slider-pilot-badge")).toHaveTextContent("SLIDER STANDARD — APPROVED / LOCKED");
    const slider = screen.getByTestId("ui-standard-slider-pg-field");
    expect(slider).toHaveAttribute("aria-valuenow", "40");
    slider.focus();
    await user.keyboard("{ArrowRight}");
    expect(slider).toHaveAttribute("aria-valuenow", "41");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext slider");
    await choosePlaygroundDropdown(user, "ui-standard-slider-pg-intent", "primary");
    await user.click(screen.getByTestId("ui-standard-slider-pg-markers"));
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext slider primary markers");
    await user.click(screen.getByTestId("ui-standards-filter-range"));
    expect(screen.getByTestId("ui-standard-card-range-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-range-standard-min")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-range-pilot-badge")).toHaveTextContent("RANGE STANDARD — APPROVED / LOCKED");
    expect(screen.getByTestId("ui-standard-range-pg-field-min")).toHaveAttribute("aria-valuenow", "20");
    expect(screen.getByTestId("ui-standard-range-pg-field-max")).toHaveAttribute("aria-valuenow", "70");
    screen.getByTestId("ui-standard-range-pg-field-max").focus();
    await user.keyboard("{ArrowRight}");
    expect(screen.getByTestId("ui-standard-range-pg-field-max")).toHaveAttribute("aria-valuenow", "71");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext range");
    await user.click(screen.getByTestId("ui-standard-range-pg-drag"));
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext range drag");
  });

  it("opens the Date picker under Form Components", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-date"));
    expect(screen.getByTestId("ui-standard-card-date-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-date")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-date")).not.toHaveTextContent("Force dark mode");
    expect(screen.getByTestId("ui-standard-date-intents-field")).toHaveTextContent("Standard, Primary, Secondary");
    expect(screen.getByTestId("ui-standard-date-raised-standard")).toHaveClass("ui-standards-qdate--raised");
    expect(screen.getByTestId("ui-standard-date-gradient-standard")).toHaveClass("ui-standards-qdate--gradient");
    expect(screen.getByTestId("ui-standard-date-flat-standard")).toHaveClass("ui-standards-qdate--flat");
    expect(screen.getByTestId("ui-standard-date-roll-standard")).toHaveClass("ui-standards-qdate--roll");
    expect(screen.getByTestId("ui-standard-date-roll-standard-roll-month")).toHaveTextContent("October");
    expect(screen.getByTestId("ui-standard-date-primary")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-date-secondary")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-date-success")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-date-today-intents-field")).toHaveTextContent("Standard, Primary, Secondary");
    expect(screen.queryByTestId("ui-standard-date-today-success")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-date-range-intents-field")).toHaveTextContent("Standard, Primary, Secondary");
    expect(screen.queryByTestId("ui-standard-date-range-success")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-date-disabled-intents-field")).toHaveTextContent("Standard, Primary, Secondary");
    expect(screen.queryByTestId("ui-standard-date-disabled-success")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-date-intents-field"));
    await user.click(screen.getByTestId("ui-standard-date-intents-option-success"));
    expect(screen.getByTestId("ui-standard-date-success")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-date-pg-field")).not.toHaveClass("ui-standards-qdate--today");
    expect(screen.getByTestId("ui-standard-date-pilot-badge")).toHaveTextContent("DATE PICKER STANDARD — APPROVED / LOCKED");
    expect(screen.getByTestId("ui-standard-date-pg-calendar-card")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-date-pg-input-card")).toHaveTextContent("PREVIEW INPUT");
    expect(screen.getByTestId("ui-standard-date-pg-datetime-card")).toHaveTextContent("PREVIEW DATE AND TIME");
    expect(within(screen.getByTestId("ui-standard-date-pg-datetime")).getByRole("textbox")).toHaveValue("2026-10-14 9:30 AM");
    expect(screen.getByTestId("ui-standard-date-pg-datetime-command")).toHaveAttribute("data-command", "ext date time");
    expect(screen.queryByTestId("ui-standard-date-pg-input-calendar")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-date-pg-input"));
    expect(screen.getByTestId("ui-standard-date-pg-input-calendar")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-date-pg-input-pop")).toHaveAttribute("data-transition", "expand");
    await choosePlaygroundDropdown(user, "ui-standard-date-pg-transition", "flip");
    expect(screen.getByTestId("ui-standard-date-pg-input-pop")).toHaveAttribute("data-transition", "flip");
    expect(screen.getByTestId("ui-standard-date-pg-input-command")).toHaveAttribute("data-command", "ext date input flip");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext date");
    await choosePlaygroundDropdown(user, "ui-standard-date-pg-transition", "expand");
    await choosePlaygroundDropdown(user, "ui-standard-date-pg-input-appearance", "outlined");
    expect(screen.getByTestId("ui-standard-date-pg-input")).toHaveAttribute("data-appearance", "outlined");
    expect(screen.getByTestId("ui-standard-date-pg-input-command")).toHaveAttribute("data-command", "ext date input outlined");
    await choosePlaygroundDropdown(user, "ui-standard-date-pg-input-appearance", "standard");
    await user.click(screen.getByTestId("ui-standard-date-pg-input-calendar-day-14"));
    expect(screen.getByTestId("ui-standard-date-pg-input-pop")).toHaveAttribute("data-state", "closed");
    await user.click(screen.getByTestId("ui-standard-date-pg-datetime"));
    expect(screen.getByTestId("ui-standard-date-pg-datetime-calendar")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-date-pg-datetime-clock")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-date-pg-datetime-calendar-day-14"));
    expect(screen.getByTestId("ui-standard-date-pg-datetime-pop")).toHaveAttribute("data-state", "open");
    await user.click(screen.getByTestId("ui-standard-date-pg-datetime-clock-hour-10"));
    await user.click(screen.getByTestId("ui-standard-date-pg-datetime-clock-minute-00"));
    expect(screen.getByTestId("ui-standard-date-pg-datetime-pop")).toHaveAttribute("data-state", "closed");
    expect(within(screen.getByTestId("ui-standard-date-pg-datetime")).getByRole("textbox")).toHaveValue("2026-10-14 10:00 AM");
    expect(screen.getByTestId("ui-standard-date-pg-field-month")).toHaveTextContent("October");
    expect(screen.getByTestId("ui-standard-date-pg-field-year")).toHaveTextContent("2026");
    expect(screen.getByTestId("ui-standard-date-pg-field-day-14")).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByTestId("ui-standard-date-pg-field-value")).toHaveTextContent("2026-10-14");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-day-20"));
    expect(screen.getByTestId("ui-standard-date-pg-field-day-20")).toHaveAttribute("aria-pressed", "true");
    expect(screen.getByTestId("ui-standard-date-pg-field")).toHaveTextContent("Oct 20");
    expect(screen.getByTestId("ui-standard-date-pg-field-value")).toHaveTextContent("2026-10-20");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-next"));
    expect(screen.getByTestId("ui-standard-date-pg-field-month")).toHaveTextContent("November");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext date");
    await choosePlaygroundDropdown(user, "ui-standard-date-pg-intent", "primary");
    await user.click(screen.getByTestId("ui-standard-date-pg-today"));
    expect(screen.getByTestId("ui-standard-date-pg-field-today")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-date-pg-field")).toHaveClass("ui-standards-qdate--today");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext date primary today");
    await user.click(screen.getByTestId("ui-standard-date-pg-range"));
    expect(screen.getByTestId("ui-standard-date-pg-field-month")).toHaveTextContent("October");
    expect(screen.getByTestId("ui-standard-date-pg-field-year")).toHaveTextContent("2026");
    expect(screen.getByTestId("ui-standard-date-pg-field-value")).toHaveTextContent("2026-10-14 – 2026-10-20");
    expect(screen.getByTestId("ui-standard-date-pg-field-day-16")).toHaveClass("ui-standards-qdate__day--in-range");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-day-8"));
    await user.click(screen.getByTestId("ui-standard-date-pg-field-day-22"));
    expect(screen.getByTestId("ui-standard-date-pg-field-value")).toHaveTextContent("2026-10-08 – 2026-10-22");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext date primary today range");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-year"));
    expect(screen.getByTestId("ui-standard-date-pg-field-year-range")).toHaveTextContent("2020 – 2039");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-year-2030"));
    expect(screen.getByTestId("ui-standard-date-pg-field-month-3")).toHaveAttribute("aria-pressed", "false");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-month-3"));
    expect(screen.getByTestId("ui-standard-date-pg-field-month")).toHaveTextContent("March");
    expect(screen.getByTestId("ui-standard-date-pg-field-year")).toHaveTextContent("2030");
    expect(screen.getByTestId("ui-standard-date-pg-field-day-1")).toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-date-pg-appearance", "raised");
    expect(screen.getByTestId("ui-standard-date-pg-field")).toHaveClass("ui-standards-qdate--raised");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext date primary raised today range");
    await choosePlaygroundDropdown(user, "ui-standard-date-pg-variety", "roll");
    expect(screen.getByTestId("ui-standard-date-pg-field")).toHaveClass("ui-standards-qdate--roll");
    expect(screen.getByTestId("ui-standard-date-pg-field-from-roll-month")).toHaveTextContent("October");
    expect(screen.getByTestId("ui-standard-date-pg-field-from-roll-day")).toHaveTextContent("8");
    expect(screen.getByTestId("ui-standard-date-pg-field-to-roll-day")).toHaveTextContent("22");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-from-roll-day-next"));
    expect(screen.getByTestId("ui-standard-date-pg-field-value")).toHaveTextContent("2026-10-09 – 2026-10-22");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-from-roll-month-next"));
    expect(screen.getByTestId("ui-standard-date-pg-field-from-roll-month")).toHaveTextContent("November");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext date primary raised roll today range");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-from-roll-month"));
    await user.click(screen.getByTestId("ui-standard-date-pg-field-from-pick-month-3"));
    expect(screen.getByTestId("ui-standard-date-pg-field-from-roll-month")).toHaveTextContent("March");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-from-roll-year"));
    expect(screen.getByTestId("ui-standard-date-pg-field-from-pick-year-range")).toHaveTextContent("2020 – 2039");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-from-pick-year-2030"));
    expect(screen.getByTestId("ui-standard-date-pg-field-from-roll-year")).toHaveTextContent("2030");
    await user.click(screen.getByTestId("ui-standard-date-pg-field-from-roll-day"));
    await user.click(screen.getByTestId("ui-standard-date-pg-field-from-pick-day-15"));
    expect(screen.getByTestId("ui-standard-date-pg-field-from-roll-day")).toHaveTextContent("15");
    expect(screen.getByTestId("ui-standard-date-pg-field-value")).toHaveTextContent("2030-03-15 – 2030-03-15");
  });

  it("snaps the clock hand to any minute", () => {
    expect(minuteFromClockDegrees(0)).toBe(0);
    expect(minuteFromClockDegrees(6)).toBe(1);
    expect(minuteFromClockDegrees(12)).toBe(2);
    expect(minuteFromClockDegrees(359)).toBe(0);
    expect(hourFromClockDegrees(0, false)).toBe(0);
    expect(hourFromClockDegrees(0, true)).toBe(12);
    expect(hourFromClockDegrees(30, false)).toBe(1);
  });

  it("opens the Time picker under Form Components", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-filter-time"));
    expect(screen.getByTestId("ui-standard-card-time-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-time")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-time-pg-field-value")).toHaveTextContent("09:30");
    expect(screen.getByTestId("ui-standard-time-pg-field")).toHaveTextContent("9:30 AM");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext time");
    await user.click(screen.getByTestId("ui-standard-time-pg-field-hour-10"));
    await user.click(screen.getByTestId("ui-standard-time-pg-field-minute-00"));
    expect(screen.getByTestId("ui-standard-time-pg-field-value")).toHaveTextContent("10:00");
    expect(screen.getByTestId("ui-standard-time-pg-input-card")).toHaveTextContent("PREVIEW INPUT");
    await user.click(screen.getByTestId("ui-standard-time-pg-input"));
    expect(screen.getByTestId("ui-standard-time-pg-input-clock")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-time-pg-input-clock-hour-11"));
    await user.click(screen.getByTestId("ui-standard-time-pg-input-clock-minute-15"));
    expect(screen.getByTestId("ui-standard-time-pg-input-pop")).toHaveAttribute("data-state", "closed");
    expect(screen.getByTestId("ui-standard-time-pg-field-value")).toHaveTextContent("11:15");
    expect(within(screen.getByTestId("ui-standard-time-pg-input")).getByRole("textbox")).toHaveValue("11:15 AM");
    expect(screen.getByTestId("ui-standard-time-pg-input-icon")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-time-pg-input-clear"));
    expect(within(screen.getByTestId("ui-standard-time-pg-input")).getByRole("textbox")).toHaveValue("");
    expect(screen.queryByTestId("ui-standard-time-pg-input-clear")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-time-pg-input-icon")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-time-pg-field-hour-11"));
    await user.click(screen.getByTestId("ui-standard-time-pg-field-minute-15"));
    expect(within(screen.getByTestId("ui-standard-time-pg-input")).getByRole("textbox")).toHaveValue("11:15 AM");
    await user.click(screen.getByTestId("ui-standard-time-pg-24h"));
    expect(screen.getByTestId("ui-standard-time-pg-field")).toHaveAttribute("data-format", "24");
    expect(screen.queryByTestId("ui-standard-time-pg-field-period-am")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-time-pg-field-hour-1")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-time-pg-field-hour-01")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext time 24h");
    expect(screen.getByTestId("ui-standard-time-pg-field")).toHaveTextContent("11:15");
    expect(within(screen.getByTestId("ui-standard-time-pg-input")).getByRole("textbox")).toHaveValue("11:15");
    await user.click(screen.getByTestId("ui-standard-time-pg-now"));
    await user.click(screen.getByTestId("ui-standard-time-pg-field-now"));
    expect(screen.getByTestId("ui-standard-time-pg-field-value")).toHaveTextContent("09:30");
    expect(screen.getByTestId("ui-standards-copy-command")).toHaveAttribute("data-command", "ext time 24h now");
    expect(screen.getByTestId("ui-standard-time-pg-input-command")).toHaveAttribute("data-command", "ext time input");
  });

  it("opens Input filter with a playground card and an input samples card", async () => {
    const user = userEvent.setup();
    renderPage("/ui-standards?category=inputs");
    expect(screen.getByTestId("ui-standards-filter-inputs")).toHaveTextContent("Input");
    expect(screen.getByTestId("ui-standards-filter-inputs")).toHaveAttribute("data-selected", "true");
    expect(screen.queryByTestId("ui-standards-preference-preview")).not.toBeInTheDocument();
    const samples = screen.getByTestId("ui-standard-cards");
    const playgroundCard = within(samples).getByTestId("ui-standard-card-input-playground");
    const inputsCard = within(samples).getByTestId("ui-standard-card-inputs");
    expect(playgroundCard.compareDocumentPosition(inputsCard) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(playgroundCard).getByText("Input playground")).toBeInTheDocument();
    expect(within(inputsCard).getByText("Input samples")).toBeInTheDocument();
    expect(within(inputsCard).getByTestId("ui-standard-input-pilot-badge")).toHaveTextContent(
      "INPUT STANDARD — APPROVED / LOCKED",
    );
    expect(within(inputsCard).getByTestId("ui-standard-input-pilot-lede")).toHaveTextContent("ext input");
    const typesCard = within(samples).getByTestId("ui-standard-card-input-types");
    expect(inputsCard.compareDocumentPosition(typesCard) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(typesCard).getAllByText("Input types").length).toBeGreaterThan(0);
    expect(within(typesCard).getByLabelText("Password")).toHaveAttribute("type", "password");
    expect(within(typesCard).getByTestId("ui-standard-input-type-password-toggle")).toBeInTheDocument();
    expect(within(typesCard).getByText("Sample: name@email.com")).toBeInTheDocument();
    expect(within(typesCard).getByText("+63 9XX XXX XXXX")).toBeInTheDocument();
    expect(within(typesCard).getByText("09XX XXX XXXX")).toBeInTheDocument();
    expect(within(typesCard).getByLabelText("Textarea").tagName).toBe("TEXTAREA");
    await user.type(within(typesCard).getByLabelText("Email"), "not-an-email");
    expect(within(typesCard).getByText("Enter a valid email address.")).toBeInTheDocument();
    await user.type(within(typesCard).getByLabelText("Mobile local"), "09171234567");
    expect(within(typesCard).getByLabelText("Mobile local")).toHaveValue("0917 123 4567");
    await user.type(within(typesCard).getByLabelText("Mobile international"), "9171234567");
    expect(within(typesCard).getByLabelText("Mobile international")).toHaveValue("+63 917 123 4567");
    expect(within(playgroundCard).queryByTestId("ui-standard-input-pg-type")).not.toBeInTheDocument();
    expect(within(inputsCard).getByTestId("ui-standard-input-standard")).toBeInTheDocument();
    expect(within(inputsCard).getByTestId("ui-standard-input-soft-outline-primary")).toHaveAttribute(
      "data-appearance",
      "soft-outline",
    );
    expect(within(inputsCard).getByTestId("ui-standard-input-outlined")).toBeInTheDocument();
    expect(within(inputsCard).getByTestId("ui-standard-input-outlined-primary")).toHaveAttribute(
      "data-appearance",
      "outlined",
    );
    expect(screen.queryByTestId("ui-standard-card-buttons")).not.toBeInTheDocument();
    const playground = screen.getByTestId("ui-standard-input-playground");
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext input primary outlined",
    );
    await user.click(within(playground).getByTestId("ui-standards-copy-command-expand"));
    expect(within(playground).getByText(/Use the locked ExItS Input Standard/)).toBeInTheDocument();
    await choosePlaygroundDropdown(user, "ui-standard-input-pg-intent", "primary");
    await choosePlaygroundDropdown(user, "ui-standard-input-pg-appearance", "outlined");
    await choosePlaygroundDropdown(user, "ui-standard-input-pg-shape", "pill");
    expect(screen.getByTestId("ui-standard-input-pg-field")).toHaveAttribute("data-appearance", "outlined");
    expect(screen.getByTestId("ui-standard-input-pg-field")).toHaveAttribute("data-shape", "pill");
    const inputAppearance = await openPlaygroundDropdown(user, "ui-standard-input-pg-appearance");
    expect(within(inputAppearance).getByTestId("ui-standard-input-pg-appearance-option-standard")).toBeDisabled();
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext input primary outlined pill",
    );
    const labelInput = screen.getByTestId("ui-standard-input-pg-label");
    await user.clear(labelInput);
    await user.type(labelInput, "SAVE");
    expect(within(screen.getByTestId("ui-standard-input-pg-preview")).getByLabelText("SAVE")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-input-pg-reset"));
    expect(within(playground).getByTestId("ui-standards-copy-command")).toHaveAttribute(
      "data-command",
      "ext input primary outlined",
    );
    expect(labelInput).toHaveValue("Standard");
  });

  it("opens Breadcrumbs filter with basic and custom separator only", () => {
    renderPage("/ui-standards?category=breadcrumbs");
    expect(screen.getByTestId("ui-standards-filter-breadcrumbs")).toHaveTextContent("Breadcrumbs");
    expect(screen.getByTestId("ui-standards-filter-breadcrumbs")).toHaveAttribute("data-selected", "true");
    const card = screen.getByTestId("ui-standard-card-breadcrumbs");
    const basic = within(card).getByTestId("ui-standard-breadcrumb-basic");
    const custom = within(card).getByTestId("ui-standard-breadcrumb-separator");
    expect(within(basic).getByText("Basic")).toBeInTheDocument();
    expect(within(custom).getByText("Custom separators")).toBeInTheDocument();
    const basicText = within(basic).getByTestId("ui-standard-breadcrumb-basic-text");
    expect(within(basicText).getByText("Home").closest("li")).toHaveClass("ui-standards-qbreadcrumbs__el--link");
    expect(within(basicText).getByText("Components").closest("li")).toHaveClass("ui-standards-qbreadcrumbs__el--link");
    expect(within(basicText).getByText("Breadcrumbs").closest("li")).not.toHaveClass("ui-standards-qbreadcrumbs__el--link");
    expect(within(basicText).getByText("Breadcrumbs")).toHaveAttribute("aria-current", "page");
    expect(within(basicText).getAllByText("/")).toHaveLength(2);
    expect(within(basic).getByTestId("ui-standard-breadcrumb-basic-icons")).toBeInTheDocument();
    expect(within(basic).getByLabelText("Navigation")).toBeInTheDocument();
    const dashes = within(custom).getByTestId("ui-standard-breadcrumb-separator-dashes");
    expect(within(dashes).getAllByText("---")).toHaveLength(2);
    expect(within(custom).getByTestId("ui-standard-breadcrumb-separator-arrow")).toBeInTheDocument();
    expect(within(custom).getByTestId("ui-standard-breadcrumb-separator-chevron")).toBeInTheDocument();
    expect(screen.queryByText("Electronics")).not.toBeInTheDocument();
    expect(screen.queryByText("Gutters")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-card-inputs")).not.toBeInTheDocument();
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
