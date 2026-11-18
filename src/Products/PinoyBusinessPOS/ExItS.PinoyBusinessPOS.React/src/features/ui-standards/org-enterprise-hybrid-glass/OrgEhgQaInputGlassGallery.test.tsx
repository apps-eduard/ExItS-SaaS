import type { ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgQaInputGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQaInputGlassGallery";
import { ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-input-glass-global-default";

function wrap(ui: ReactNode) {
  return (
    <MemoryRouter>
      <PreferencesProvider>
        <ToastProvider>{ui}</ToastProvider>
      </PreferencesProvider>
    </MemoryRouter>
  );
}

describe("OrgEhgQaInputGlassGallery", () => {
  it("renders playground and light/dark panels with QA glass recipe", () => {
    render(wrap(<OrgEhgQaInputGlassGallery />));
    const root = screen.getByTestId("ui-standard-org-ehg-qa-input-glass");
    expect(
      within(root).getByTestId("ui-standard-card-org-ehg-qa-input-glass-playground"),
    ).toBeInTheDocument();
    expect(within(root).getByTestId("org-ehg-qa-input-glass-light")).toBeInTheDocument();
    expect(within(root).getByTestId("org-ehg-qa-input-glass-dark")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-input-pg-preview-field");
    expect(preview).toHaveClass("org-ehg-qa-input-glass");
    expect(preview.querySelector(".org-ehg-qa-input-glass__field")).toHaveClass(
      "org-ehg-qa-glass",
      "org-ehg-qa-input-glass--density-balance",
    );
    expect(preview).toHaveAttribute("data-density", "balance");
    expect(preview).toHaveAttribute("data-appearance", "stacked");
    expect(preview).toHaveAttribute("data-card-shape", "standard");
    expect(preview).toHaveAttribute("data-has-icon", "false");
    expect(preview.querySelector(".org-ehg-qa-input-glass__field")).toHaveClass(
      "org-ehg-qa-glass--no-shadow",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-card-shape")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-appearance")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-icon-appearance")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-density")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-shadow")).not.toBeChecked();
    expect(screen.queryByTestId("ui-standard-org-ehg-qa-input-pg-preview-chip")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-copy")).toHaveTextContent(
      ORG_EHG_INPUT_GLASS_DEFAULT_COMMAND,
    );
  });

  it("applies Appearance Soft to the preview field", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaInputGlassGallery />));
    const appearanceRoot = screen.getByTestId("ui-standard-org-ehg-qa-input-pg-appearance");
    await user.click(
      within(appearanceRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-appearance-field"),
    );
    await user.click(
      within(appearanceRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-appearance-option-soft"),
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-preview-field")).toHaveAttribute(
      "data-appearance",
      "soft",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-copy")).toHaveTextContent(" soft ");
  });

  it("keeps Label above inside the glass field like normal Input stacked", () => {
    render(wrap(<OrgEhgQaInputGlassGallery />));
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-input-pg-preview-field");
    expect(preview).toHaveAttribute("data-appearance", "stacked");
    const label = preview.querySelector(".org-ehg-qa-input-glass__field .org-ehg-qa-input-glass__label");
    expect(label).toBeTruthy();
    expect(label).toHaveTextContent("Input");
    expect(preview.querySelector(".org-ehg-qa-input-glass__label--above")).toBeNull();
  });

  it("applies density from the Density dropdown", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaInputGlassGallery />));
    const densityRoot = screen.getByTestId("ui-standard-org-ehg-qa-input-pg-density");
    await user.click(within(densityRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-density-field"));
    await user.click(
      within(densityRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-density-option-compact"),
    );
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-input-pg-preview-field");
    expect(preview).toHaveAttribute("data-density", "compact");
    expect(preview.querySelector(".org-ehg-qa-input-glass__field")).toHaveClass(
      "org-ehg-qa-input-glass--density-compact",
    );
  });

  it("keeps Standard in the middle and can select Small", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaInputGlassGallery />));
    const shapeRoot = screen.getByTestId("ui-standard-org-ehg-qa-input-pg-card-shape");
    await user.click(within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-card-shape-field"));
    expect(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-card-shape-option-small"),
    ).toBeInTheDocument();
    expect(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-card-shape-option-standard"),
    ).toBeInTheDocument();
    expect(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-card-shape-option-soft"),
    ).toBeInTheDocument();
    await user.click(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-card-shape-option-small"),
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-preview-field")).toHaveAttribute(
      "data-card-shape",
      "small",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-copy")).toHaveTextContent(
      "shape-small",
    );
  });

  it("applies Icon appearance Ghost to the preview chip", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaInputGlassGallery />));
    const iconAppearanceRoot = screen.getByTestId("ui-standard-org-ehg-qa-input-pg-icon-appearance");
    await user.click(
      within(iconAppearanceRoot).getByTestId("ui-standard-org-ehg-qa-input-pg-icon-appearance-field"),
    );
    await user.click(
      within(iconAppearanceRoot).getByTestId(
        "ui-standard-org-ehg-qa-input-pg-icon-appearance-option-ghost",
      ),
    );
    const chip = screen.getByTestId("ui-standard-org-ehg-qa-input-pg-preview-chip");
    expect(chip).toHaveClass("org-ehg-qa-glass__icon--ghost");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-input-pg-copy")).toHaveTextContent("icon-ghost");
  });
});
