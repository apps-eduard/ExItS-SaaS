import { describe, expect, it, vi } from "vitest";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { catalogs } from "@/i18n/messages";
import { ShellUiStandardsButton } from "@/components/exits/ShellUiStandardsButton";
import { UiStandardsPage } from "@/features/ui-standards/UiStandardsPage";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: keyof typeof catalogs.en) => catalogs.en[key] ?? String(key),
  }),
}));

vi.mock("@/api/platform/local-validation-gate", () => ({
  isFrontendLocalValidationMode: () => true,
}));

describe("UiStandardsPage", () => {
  it("renders tables reference with ExitsTable and switches to button samples", async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter initialEntries={["/ui-standards"]}>
        <Routes>
          <Route path="/ui-standards" element={<UiStandardsPage />} />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByTestId("ui-standards-page")).toBeInTheDocument();
    expect(screen.getByText("ExItS UI Standards")).toBeInTheDocument();
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
    expect(screen.getByTestId("ui-standards-btn-group-outline")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-ghost")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-info")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-warning")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-danger")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-danger-strong")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-icon-only")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-icon-only-round-intents")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-btn-group-states")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-button-motion")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-icon-edit-round")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-round-primary")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-motion-round-refresh")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standards-motion-success-demo")).toBeInTheDocument();
    const samples = within(screen.getByTestId("ui-standards-button-showcase"));
    expect(samples.getByRole("button", { name: "Save" })).toBeInTheDocument();
    expect(samples.getByRole("button", { name: "Approve" })).toBeInTheDocument();
    expect(samples.getByRole("button", { name: "Delete permanently" })).toBeInTheDocument();
    expect(samples.getAllByLabelText("Edit").length).toBeGreaterThanOrEqual(3);
    expect(samples.getAllByLabelText("Refresh").length).toBeGreaterThanOrEqual(1);
    expect(screen.getByTestId("ui-standards-button-cheatsheet")).toHaveTextContent("SHAPE");
    expect(screen.getByTestId("ui-standards-button-cheatsheet")).toHaveTextContent("TREATMENT");
    expect(screen.getByTestId("ui-standards-button-cheatsheet")).toHaveTextContent("ICON ONLY ROUND");
    expect(screen.getByTestId("ui-standards-button-cheatsheet")).toHaveTextContent("ROUND (icon-only circle)");
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
