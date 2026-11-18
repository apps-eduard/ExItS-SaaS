import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import type { ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgQaBtnGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQaBtnGlassGallery";
import { ORG_EHG_BTN_GLASS_DEFAULT_COMMAND } from "@/features/ui-standards/org-enterprise-hybrid-glass/org-ehg-btn-glass-global-default";

const css = readFileSync(
  resolve(__dirname, "org-enterprise-hybrid-glass.css"),
  "utf8",
);

function wrap(ui: ReactNode) {
  return (
    <MemoryRouter>
      <PreferencesProvider>
        <ToastProvider>{ui}</ToastProvider>
      </PreferencesProvider>
    </MemoryRouter>
  );
}

describe("OrgEhgQaBtnGlassGallery", () => {
  it("renders playground and light/dark panels with QA glass recipe", () => {
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    const root = screen.getByTestId("ui-standard-org-ehg-qa-btn-glass");
    expect(within(root).getByTestId("ui-standard-card-org-ehg-qa-btn-glass-playground")).toBeInTheDocument();
    expect(within(root).getByTestId("org-ehg-qa-btn-glass-light")).toBeInTheDocument();
    expect(within(root).getByTestId("org-ehg-qa-btn-glass-dark")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-btn");
    expect(preview).toHaveClass(
      "org-ehg-qa-glass",
      "org-ehg-qa-btn-glass",
      "org-ehg-qa-glass--no-shadow",
      "org-ehg-qa-btn-glass--appearance-flat",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-density")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-appearance")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-content-align")).toBeInTheDocument();
    expect(preview).toHaveAttribute("data-density", "balance");
    expect(preview).toHaveAttribute("data-appearance", "flat");
    expect(preview).toHaveAttribute("data-content-align", "center");
    expect(preview).toHaveAttribute("data-hover", "on");
    expect(preview).toHaveAttribute("data-card-shape", "standard");
    expect(preview).toHaveClass("org-ehg-qa-btn-glass--align-center");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-shadow")).not.toBeChecked();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-shadow")).not.toBeChecked();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-hover")).toBeChecked();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-card-shape")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-org-ehg-qa-btn-pg-preview-chip")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(
      ORG_EHG_BTN_GLASS_DEFAULT_COMMAND,
    );
  });

  it("applies Icon appearance Ghost to the preview chip", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    const iconAppearanceRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance");
    await user.click(
      within(iconAppearanceRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance-field"),
    );
    await user.click(
      within(iconAppearanceRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance-option-ghost"),
    );
    const chip = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-chip");
    expect(chip).toHaveClass("org-ehg-qa-glass__icon--ghost");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent("icon-ghost");
  });

  it("keeps Icon appearance None with no icon mark by default", () => {
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    expect(screen.queryByTestId("ui-standard-org-ehg-qa-btn-pg-preview-chip")).not.toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-org-ehg-qa-btn-pg-preview-bare")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent("icon-none");
  });

  it("applies Icon appearance Bare as glyph only", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    const iconAppearanceRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance");
    await user.click(
      within(iconAppearanceRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance-field"),
    );
    await user.click(
      within(iconAppearanceRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance-option-bare"),
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-bare")).toBeInTheDocument();
    expect(screen.queryByTestId("ui-standard-org-ehg-qa-btn-pg-preview-chip")).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent("icon-bare");
  });

  it.each([
    ["outline", "org-ehg-qa-glass__icon--outline", "icon-outline"],
    ["soft", "org-ehg-qa-glass__icon--soft-fill", "icon-soft-fill"],
    ["glossy", "org-ehg-qa-glass__icon--glossy", "icon-glossy"],
  ] as const)("applies Icon appearance %s", async (value, chipClass, commandToken) => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    const iconAppearanceRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance");
    await user.click(
      within(iconAppearanceRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance-field"),
    );
    await user.click(
      within(iconAppearanceRoot).getByTestId(
        `ui-standard-org-ehg-qa-btn-pg-icon-appearance-option-${value}`,
      ),
    );
    const chip = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-chip");
    expect(chip).toHaveClass(chipClass);
    expect(chip).toHaveAttribute("data-icon-appearance", value);
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(commandToken);
  });

  it("applies density from the Density dropdown", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    const densityRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-density");
    await user.click(within(densityRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-density-field"));
    await user.click(within(densityRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-density-option-compact"));
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-btn");
    expect(preview).toHaveAttribute("data-density", "compact");
    expect(preview).toHaveClass("org-ehg-qa-btn-glass--density-compact");
  });

  it("seeds Appearance to flat and applies glossy from the dropdown", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-btn");
    expect(preview).toHaveAttribute("data-appearance", "flat");
    expect(preview).toHaveClass("org-ehg-qa-btn-glass--appearance-flat");
    const appearanceRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-appearance");
    await user.click(within(appearanceRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-appearance-field"));
    await user.click(
      within(appearanceRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-appearance-option-glossy"),
    );
    expect(preview).toHaveAttribute("data-appearance", "glossy");
    expect(preview).toHaveClass("org-ehg-qa-btn-glass--appearance-glossy");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent("glossy");
  });

  it("applies Content align End to icon and label", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    const alignRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-content-align");
    await user.click(within(alignRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-content-align-field"));
    await user.click(
      within(alignRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-content-align-option-end"),
    );
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-btn");
    expect(preview).toHaveAttribute("data-content-align", "end");
    expect(preview).toHaveClass("org-ehg-qa-btn-glass--align-end");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent("align-end");
  });

  it("removes drop-shadow class when Shadow is off and restores when on", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-btn");
    expect(preview).toHaveClass("org-ehg-qa-glass--no-shadow");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-shadow")).not.toBeChecked();
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-shadow"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-shadow")).toBeChecked();
    expect(preview).not.toHaveClass("org-ehg-qa-glass--no-shadow");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).not.toHaveTextContent(
      "no-shadow",
    );
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-shadow"));
    expect(preview).toHaveClass("org-ehg-qa-glass--no-shadow");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent("no-shadow");
  });

  it("keeps hover elevation when Shadow is off", () => {
    expect(css).toContain(
      '.org-ehg-qa-btn-glass.org-ehg-qa-glass--no-shadow:not([data-hover="off"]):hover:not(:disabled)',
    );
    expect(css).toMatch(
      /\.org-ehg-qa-btn-glass\.org-ehg-qa-glass--no-shadow:not\(\[data-hover="off"\]\):hover:not\(:disabled\)\s*\{[^}]*0 10px 26px/,
    );
  });

  it("can turn Hover off and select Shape Small", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQaBtnGlassGallery />));
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-hover"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-btn")).toHaveAttribute(
      "data-hover",
      "off",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent("no-hover");
    const shapeRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-card-shape");
    await user.click(within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-card-shape-field"));
    await user.click(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-card-shape-option-small"),
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-btn")).toHaveAttribute(
      "data-card-shape",
      "small",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent("shape-small");
  });
});
