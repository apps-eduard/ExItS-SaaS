import type { ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgQuickActionGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";
import { OrgEhgQuickActionGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassPlayground";

function wrap(ui: ReactNode) {
  return (
    <MemoryRouter>
      <PreferencesProvider>
        <ToastProvider>{ui}</ToastProvider>
      </PreferencesProvider>
    </MemoryRouter>
  );
}

describe("OrgEhgQuickActionGlassPlayground", () => {
  it("renders controls and preview", () => {
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    expect(screen.getByTestId("ui-standard-card-org-ehg-qa-glass-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-tone")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-trailing")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-size")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-card-shape")).toBeInTheDocument();
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn");
    expect(preview).toBeInTheDocument();
    expect(preview).toHaveClass("org-ehg-qa-glass--size-md");
    expect(preview).toHaveAttribute("data-size", "md");
    expect(preview).toHaveAttribute("data-card-shape", "standard");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-copy")).toBeInTheDocument();
  });

  it("switches card shape on the preview row", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    const shapeRoot = screen.getByTestId("ui-standard-org-ehg-qa-pg-card-shape");
    await user.click(within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-pg-card-shape-field"));
    await user.click(within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-pg-card-shape-option-pill"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn")).toHaveAttribute(
      "data-card-shape",
      "pill",
    );
  });

  it("toggles shadow off on the preview row", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    const preview = screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn");
    expect(preview).not.toHaveClass("org-ehg-qa-glass--no-shadow");
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-pg-shadow"));
    expect(preview).toHaveClass("org-ehg-qa-glass--no-shadow");
  });

  it("toggles icon shadow off on icon-only preview", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-pg-icon-only"));
    const icon = screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-icon");
    expect(icon).not.toHaveClass("org-ehg-qa-glass-icon--no-shadow");
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-pg-icon-shadow"));
    expect(icon).toHaveClass("org-ehg-qa-glass-icon--no-shadow");
  });

  it("switches size sm / lg on the preview row", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    const sizeRoot = screen.getByTestId("ui-standard-org-ehg-qa-pg-size");
    await user.click(within(sizeRoot).getByTestId("ui-standard-org-ehg-qa-pg-size-field"));
    await user.click(within(sizeRoot).getByTestId("ui-standard-org-ehg-qa-pg-size-option-sm"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn")).toHaveClass(
      "org-ehg-qa-glass--size-sm",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn")).toHaveAttribute(
      "data-size",
      "sm",
    );

    await user.click(within(sizeRoot).getByTestId("ui-standard-org-ehg-qa-pg-size-field"));
    await user.click(within(sizeRoot).getByTestId("ui-standard-org-ehg-qa-pg-size-option-lg"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn")).toHaveClass(
      "org-ehg-qa-glass--size-lg",
    );
  });

  it("switches to icon-only preview", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-pg-icon-only"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-icon")).toBeInTheDocument();
  });

  it("action icon leads inside the Quick Action card (no solid leading chip)", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    const trailingRoot = screen.getByTestId("ui-standard-org-ehg-qa-pg-trailing");
    await user.click(within(trailingRoot).getByTestId("ui-standard-org-ehg-qa-pg-trailing-field"));
    await user.click(
      within(trailingRoot).getByTestId("ui-standard-org-ehg-qa-pg-trailing-option-action-icon"),
    );
    const card = screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn");
    expect(card).toHaveClass("org-ehg-qa-glass");
    const chip = within(card).getByTestId("ui-standard-org-ehg-qa-pg-preview-action-icon");
    expect(chip).toHaveClass("org-ehg-qa-glass-icon--action");
    expect(chip).toHaveClass("org-ehg-qa-glass-icon--tone-primary");
    expect(within(card).queryByTestId("ui-standard-org-ehg-qa-pg-preview-chip")).not.toBeInTheDocument();
  });

  it("action icon tone follows the selected card tone", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    const trailingRoot = screen.getByTestId("ui-standard-org-ehg-qa-pg-trailing");
    await user.click(within(trailingRoot).getByTestId("ui-standard-org-ehg-qa-pg-trailing-field"));
    await user.click(
      within(trailingRoot).getByTestId("ui-standard-org-ehg-qa-pg-trailing-option-action-icon"),
    );

    const toneRoot = screen.getByTestId("ui-standard-org-ehg-qa-pg-tone");
    await user.click(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-field"));
    await user.click(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-option-inventory"));

    const card = screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn");
    expect(card).toHaveClass("org-ehg-qa-glass--tone-inventory");
    expect(
      within(card).getByTestId("ui-standard-org-ehg-qa-pg-preview-action-icon"),
    ).toHaveClass("org-ehg-qa-glass-icon--tone-inventory");
  });

  it("exposes the expanded tone options including warning and info", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    const toneRoot = screen.getByTestId("ui-standard-org-ehg-qa-pg-tone");
    await user.click(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-field"));
    expect(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-option-info")).toBeInTheDocument();
    expect(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-option-warning")).toBeInTheDocument();
    expect(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-option-amber")).toBeInTheDocument();
    expect(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-option-teal")).toBeInTheDocument();
    expect(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-option-cyan")).toBeInTheDocument();
    expect(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-option-rose")).toBeInTheDocument();
    expect(within(toneRoot).getByTestId("ui-standard-org-ehg-qa-pg-tone-option-charcoal")).toBeInTheDocument();
  });

  it("action group leads inside the Quick Action card (no solid leading chip)", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    const trailingRoot = screen.getByTestId("ui-standard-org-ehg-qa-pg-trailing");
    await user.click(within(trailingRoot).getByTestId("ui-standard-org-ehg-qa-pg-trailing-field"));
    await user.click(
      within(trailingRoot).getByTestId("ui-standard-org-ehg-qa-pg-trailing-option-action-group"),
    );
    const card = screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn");
    expect(card).toHaveClass("org-ehg-qa-glass");
    expect(within(card).queryByTestId("ui-standard-org-ehg-qa-pg-preview-chip")).not.toBeInTheDocument();
    const group = within(card).getByTestId("ui-standard-org-ehg-qa-pg-preview-group");
    expect(group).toHaveAttribute("data-icon-shape", "soft");
    expect(group.querySelector(".org-ehg-qa-glass-icon--soft")).toBeTruthy();

    const shapeRoot = screen.getByTestId("ui-standard-org-ehg-qa-pg-icon-shape");
    await user.click(within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-pg-icon-shape-field"));
    await user.click(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-pg-icon-shape-option-rounded-circle"),
    );
    expect(
      within(screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-btn")).getByTestId(
        "ui-standard-org-ehg-qa-pg-preview-group",
      ),
    ).toHaveAttribute("data-icon-shape", "rounded");
    expect(
      screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-group").querySelector(
        ".org-ehg-qa-glass-icon--rounded",
      ),
    ).toBeTruthy();
  });

  it("defaults soft icon chip and can select rounded", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassPlayground />));
    const chip = screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-chip");
    expect(chip).toHaveAttribute("data-icon-shape", "soft");
    expect(chip.className).not.toMatch(/org-ehg-qa-glass__icon--rounded/);

    const shapeRoot = screen.getByTestId("ui-standard-org-ehg-qa-pg-icon-shape");
    const field = within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-pg-icon-shape-field");
    await user.click(field);
    await user.click(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-pg-icon-shape-option-rounded-circle"),
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-pg-preview-chip")).toHaveClass(
      "org-ehg-qa-glass__icon--rounded",
    );
  });

  it("is mounted from the gallery", () => {
    render(wrap(<OrgEhgQuickActionGlassGallery />));
    expect(screen.getByTestId("ui-standard-card-org-ehg-qa-glass-playground")).toBeInTheDocument();
  });
});
