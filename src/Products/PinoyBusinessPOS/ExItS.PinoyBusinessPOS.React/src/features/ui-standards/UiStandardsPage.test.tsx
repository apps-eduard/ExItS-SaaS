import { beforeEach, describe, expect, it, vi } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { catalogs } from "@/i18n/messages";
import { ShellUiStandardsButton } from "@/components/exits/ShellUiStandardsButton";
import { UiStandardsPage } from "@/features/ui-standards/UiStandardsPage";
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

function renderPage() {
  return render(
    <MemoryRouter initialEntries={["/ui-standards"]}>
      <Routes>
        <Route path="/ui-standards" element={<UiStandardsPage />} />
      </Routes>
    </MemoryRouter>,
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
      "buttons.motion": true,
      "unknown.key": true as unknown as boolean,
    });
    const restored = readUiStandardsDisclosure();
    expect(restored["buttons.motion"]).toBe(true);
    expect(restored["buttons.shapes"]).toBe(UI_STANDARDS_DEFAULT_OPEN["buttons.shapes"]);
    expect(Object.keys(restored)).not.toContain("unknown.key");
  });
});

describe("UiStandardsPage", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("renders tables reference with ExitsTable and switches to button samples", async () => {
    const user = userEvent.setup();
    renderPage();

    expect(screen.getByTestId("ui-standards-page")).toBeInTheDocument();
    expect(screen.getByText("ExItS UI Standards")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-sticky-nav")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-disclosure-toolbar")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-tables-section")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-table")).toHaveClass("exits-table-container");
    expect(screen.getByTestId("exits-table-output-actions")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-select-all")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-table-cheatsheet")).toHaveTextContent("FULL TABLE");
    expect(screen.getAllByText("Apple").length).toBeGreaterThanOrEqual(1);

    await user.click(screen.getByTestId("ui-standards-tab-buttons"));
    expect(screen.getByTestId("ui-standards-buttons-section")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-button-shapes")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-button-treatments")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-shape-primary-standard")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-shape-primary-soft")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-shape-primary-pill")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-treatment-primary-flat")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-treatment-primary-elevated")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-treatment-primary-gradient")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-primary")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-success")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-muted")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-cancel-circlex")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-cancel-corner-up-left")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-cancel-x")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-shape-cancel-circlex")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-shape-cancel-corner-up-left")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-shape-cancel-x")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-button-cheatsheet")).toHaveTextContent("CANCEL ICONS");
    expect(screen.getByTestId("ui-standards-button-cheatsheet")).toHaveTextContent("APPROVED / LOCKED");
    expect(screen.getByTestId("ui-standards-button-cheatsheet")).toHaveTextContent("ICON ONLY ROUND");
    expect(screen.getByTestId("ui-standards-button-cheatsheet")).toHaveTextContent(
      "Cancel → CircleX",
    );

    const samples = within(screen.getByTestId("ui-standards-button-showcase"));
    expect(samples.getByRole("button", { name: "Save" })).toBeInTheDocument();
    expect(samples.getByRole("button", { name: "Approve" })).toBeInTheDocument();

    await user.click(screen.getByTestId("ui-standards-tab-chips"));
    expect(screen.getByTestId("ui-standards-chips-section")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-chips-shapes")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-chips-compact-tags")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-chips-status")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-chips-filter")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-chips-tags")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-chips-cheatsheet")).toHaveTextContent("PILOT / NOT LOCKED");
    expect(screen.getByTestId("ui-standards-chip-cheatsheet-body")).toHaveTextContent("STATUS CHIP");
    expect(screen.getByTestId("ui-standards-chip-cheatsheet-body")).toHaveTextContent("SQUARE");
  });

  it("toggles sections, expand/collapse/reset, and persists layout", async () => {
    const user = userEvent.setup();
    renderPage();
    await user.click(screen.getByTestId("ui-standards-tab-buttons"));

    const shapesToggle = screen.getByTestId("ui-standards-button-shapes-toggle");
    expect(shapesToggle).toHaveAttribute("aria-expanded", "true");
    await user.click(shapesToggle);
    expect(shapesToggle).toHaveAttribute("aria-expanded", "false");

    await user.click(screen.getByTestId("ui-standards-expand-all"));
    expect(screen.getByTestId("ui-standards-button-shapes-toggle")).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByTestId("ui-standards-button-motion-toggle")).toHaveAttribute(
      "aria-expanded",
      "true",
    );

    await user.click(screen.getByTestId("ui-standards-collapse-all"));
    expect(screen.getByTestId("ui-standards-button-shapes-toggle")).toHaveAttribute(
      "aria-expanded",
      "false",
    );
    expect(screen.getByTestId("ui-standards-button-motion-toggle")).toHaveAttribute(
      "aria-expanded",
      "false",
    );

    await user.click(screen.getByTestId("ui-standards-reset-layout"));
    expect(screen.getByTestId("ui-standards-button-shapes-toggle")).toHaveAttribute(
      "aria-expanded",
      "true",
    );
    expect(screen.getByTestId("ui-standards-button-motion-toggle")).toHaveAttribute(
      "aria-expanded",
      "false",
    );

    const stored = JSON.parse(
      window.localStorage.getItem(UI_STANDARDS_SECTIONS_STORAGE_KEY) ?? "{}",
    ) as Record<string, boolean>;
    expect(stored["buttons.shapes"]).toBe(true);
    expect(stored["buttons.motion"]).toBe(false);
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
