import type { ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgQaSelectGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQaSelectGlassGallery";
import { ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-select-glass-global-default";

function wrap(ui: ReactNode) {
  return (
    <MemoryRouter>
      <PreferencesProvider>
        <ToastProvider>{ui}</ToastProvider>
      </PreferencesProvider>
    </MemoryRouter>
  );
}

describe("OrgEhgQaSelectGlassGallery", () => {
  it("renders playground and light/dark panels with QA glass recipe", () => {
    render(wrap(<OrgEhgQaSelectGlassGallery />));
    const root = screen.getByTestId("ui-standard-org-ehg-qa-select-glass");
    expect(
      within(root).getByTestId("ui-standard-card-org-ehg-qa-select-glass-playground"),
    ).toBeInTheDocument();
    expect(within(root).getByTestId("org-ehg-qa-select-glass-light")).toBeInTheDocument();
    expect(within(root).getByTestId("org-ehg-qa-select-glass-dark")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-field");
    expect(preview).toHaveClass("org-ehg-qa-input-glass", "org-ehg-qa-select-glass");
    expect(preview.querySelector(".org-ehg-qa-select-glass__field")).toHaveClass(
      "org-ehg-qa-glass",
      "org-ehg-qa-input-glass--density-balance",
    );
    expect(preview).toHaveAttribute("data-density", "balance");
    expect(preview).toHaveAttribute("data-appearance", "stacked");
    expect(preview).toHaveAttribute("data-card-shape", "standard");
    expect(preview).toHaveAttribute("data-hover", "on");
    expect(preview).toHaveAttribute("data-has-icon", "false");
    expect(preview.querySelector(".org-ehg-qa-select-glass__field")).toHaveClass(
      "org-ehg-qa-glass--no-shadow",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-card-shape")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-hover")).toBeChecked();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-shadow")).not.toBeChecked();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-appearance")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-icon-appearance")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-density")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-chevron")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-copy")).toHaveTextContent(
      ORG_EHG_SELECT_GLASS_DEFAULT_COMMAND,
    );
  });

  it("opens a tone-matched glass menu for Label above", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaSelectGlassGallery />));
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-trigger"));
    const menu = screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-menu").closest(
      ".org-ehg-qa-select-glass-menu",
    );
    expect(menu).toHaveClass("org-ehg-qa-glass--tone-primary", "org-ehg-qa-select-glass-menu");
  });

  it("opens the glass menu and selects an option", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaSelectGlassGallery />));
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-trigger"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-field")).toHaveAttribute(
      "data-open",
      "true",
    );
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-option-b"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-value")).toHaveTextContent(
      "Option B",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-field")).toHaveAttribute(
      "data-raised",
      "true",
    );
  });

  it("shows one label then N Selected when multi has two picks", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaSelectGlassGallery />));
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-multi"));
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-trigger"));
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-option-a"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-value")).toHaveTextContent(
      "Option A",
    );
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-option-b"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-value")).toHaveTextContent(
      "2 Selected",
    );
  });

  it("applies Appearance Soft to the preview field", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaSelectGlassGallery />));
    const appearanceRoot = screen.getByTestId("ui-standard-org-ehg-qa-select-pg-appearance");
    await user.click(
      within(appearanceRoot).getByTestId("ui-standard-org-ehg-qa-select-pg-appearance-field"),
    );
    await user.click(
      within(appearanceRoot).getByTestId("ui-standard-org-ehg-qa-select-pg-appearance-option-soft"),
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-field")).toHaveAttribute(
      "data-appearance",
      "soft",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-copy")).toHaveTextContent(" soft ");
  });

  it("applies density from the Density dropdown", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaSelectGlassGallery />));
    const densityRoot = screen.getByTestId("ui-standard-org-ehg-qa-select-pg-density");
    await user.click(
      within(densityRoot).getByTestId("ui-standard-org-ehg-qa-select-pg-density-field"),
    );
    await user.click(
      within(densityRoot).getByTestId("ui-standard-org-ehg-qa-select-pg-density-option-compact"),
    );
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-field");
    expect(preview).toHaveAttribute("data-density", "compact");
    expect(preview.querySelector(".org-ehg-qa-select-glass__field")).toHaveClass(
      "org-ehg-qa-input-glass--density-compact",
    );
  });

  it("keeps Standard in the middle and can select Small", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaSelectGlassGallery />));
    const shapeRoot = screen.getByTestId("ui-standard-org-ehg-qa-select-pg-card-shape");
    await user.click(within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-select-pg-card-shape-field"));
    expect(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-select-pg-card-shape-option-small"),
    ).toBeInTheDocument();
    expect(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-select-pg-card-shape-option-standard"),
    ).toBeInTheDocument();
    expect(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-select-pg-card-shape-option-soft"),
    ).toBeInTheDocument();
    await user.click(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-select-pg-card-shape-option-small"),
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-preview-field")).toHaveAttribute(
      "data-card-shape",
      "small",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-select-pg-copy")).toHaveTextContent(
      "shape-small",
    );
  });
});
