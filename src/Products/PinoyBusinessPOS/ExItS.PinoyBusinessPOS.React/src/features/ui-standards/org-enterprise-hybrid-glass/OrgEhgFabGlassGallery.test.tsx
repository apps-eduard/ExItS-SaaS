import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import type { ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgFabGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgFabGlassGallery";

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

describe("OrgEhgFabGlassGallery", () => {
  it("renders floating action playground and light/dark FAB samples on QA btn glass", () => {
    render(wrap(<OrgEhgFabGlassGallery />));
    expect(screen.getByTestId("ui-standard-org-ehg-fab-glass")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-org-ehg-qa-btn-glass-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-org-ehg-qa-btn-glass-playground")).toHaveAttribute(
      "data-bg-option",
      "spotlight-glow",
    );
    const previewBtn = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-btn");
    expect(previewBtn).toHaveClass(
      "org-ehg-qa-glass-icon",
      "org-ehg-qa-glass-icon--size-lg",
      "org-ehg-qa-glass-icon--soft-fill",
    );
    expect(previewBtn).not.toHaveClass("org-ehg-qa-btn-glass");
    expect(previewBtn).toHaveAttribute("data-fab", "true");
    expect(previewBtn).toHaveAttribute("data-size", "lg");
    expect(previewBtn).toHaveAttribute("data-card-shape", "pill");
    expect(previewBtn).toHaveAttribute("data-icon-appearance", "soft");
    expect(previewBtn).toHaveClass("org-ehg-qa-glass-icon--tone-people");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-fab-scene")).toHaveAttribute(
      "data-fab-position",
      "right-bottom",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-speed-dial")).toHaveAttribute(
      "data-open",
      "true",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(
      "ext org-ehg btn-glass qa people flat lg icon-soft shape-pill fab pos-right-bottom speed-dial icons-person-item-cart no-shadow no-icon-shadow icon-soft-fill",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-fab")).toBeChecked();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-speed-dial")).toBeChecked();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-fab-position")).toBeInTheDocument();

    const light = screen.getByTestId("org-ehg-fab-glass-light");
    expect(within(light).getByTestId("org-ehg-fab-glass-light-fab")).toHaveClass(
      "org-ehg-qa-btn-glass--fab",
    );
    expect(within(light).getByTestId("org-ehg-fab-glass-light-fab-scene")).toHaveAttribute(
      "data-fab-position",
      "right-bottom",
    );
    expect(within(light).getByTestId("org-ehg-fab-glass-light-speed-dial")).toHaveAttribute(
      "data-open",
      "true",
    );
    expect(within(light).getByTestId("org-ehg-fab-glass-light-speed-dial")).toHaveAttribute(
      "data-label-type",
      "label-tooltip",
    );
    expect(within(light).getByTestId("org-ehg-fab-glass-light-speed-dial")).toHaveAttribute(
      "data-show",
      "top",
    );
    expect(within(light).getByTestId("org-ehg-fab-glass-light-speed-dial-show-left")).toHaveAttribute(
      "data-show",
      "left",
    );
    expect(within(light).getByTestId("org-ehg-fab-glass-light-speed-dial-closed")).toHaveAttribute(
      "data-open",
      "false",
    );
    expect(within(light).getByTestId("org-ehg-fab-glass-light-speed-dial-icon-only")).toHaveAttribute(
      "data-label-type",
      "icon-only",
    );
    expect(
      within(light).getByTestId("org-ehg-fab-glass-light-speed-dial-rounded-label"),
    ).toHaveAttribute("data-label-type", "rounded-label");
    expect(screen.getByTestId("org-ehg-fab-glass-dark")).toBeInTheDocument();
  });

  it("floating action is the icon itself and follows Size, Shape, Icon appearance", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgFabGlassGallery />));

    const previewBtn = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-btn");
    expect(previewBtn).toHaveClass("org-ehg-qa-glass-icon");
    expect(previewBtn).not.toHaveClass("org-ehg-qa-btn-glass", "org-ehg-qa-btn-glass--fab");
    expect(previewBtn).toHaveAttribute("data-fab", "true");
    expect(previewBtn).toHaveAttribute("data-appearance", "flat");
    expect(previewBtn).toHaveClass("org-ehg-qa-btn-glass--appearance-flat");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-appearance")).toBeInTheDocument();

    const sizeRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-size");
    await user.click(within(sizeRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-size-field"));
    await user.click(within(sizeRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-size-option-sm"));
    expect(previewBtn).toHaveAttribute("data-size", "sm");
    expect(previewBtn).toHaveClass("org-ehg-qa-glass-icon--size-sm");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(/\bsm\b/);

    const shapeRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-card-shape");
    await user.click(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-card-shape-field"),
    );
    await user.click(
      within(shapeRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-card-shape-option-square"),
    );
    expect(previewBtn).toHaveAttribute("data-card-shape", "square");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(
      "shape-square",
    );

    const iconAppearanceRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance");
    await user.click(
      within(iconAppearanceRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-icon-appearance-field"),
    );
    await user.click(
      within(iconAppearanceRoot).getByTestId(
        "ui-standard-org-ehg-qa-btn-pg-icon-appearance-option-soft",
      ),
    );
    expect(previewBtn).toHaveAttribute("data-icon-appearance", "soft");
    expect(previewBtn).toHaveClass("org-ehg-qa-glass-icon--soft-fill");
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(
      "icon-soft-fill",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(/\bfab\b/);
    expect(previewBtn).not.toHaveClass("org-ehg-qa-btn-glass");
  });

  it("speed dial icons select and Labels switch control the stacked actions", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgFabGlassGallery />));

    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-speed-dial")).toBeChecked();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-speed-dial-icons")).not.toHaveClass(
      "ui-standards-qdropdown--disabled",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-speed-dial-labels")).toBeEnabled();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-speed-dial")).toHaveAttribute(
      "data-open",
      "true",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-speed-dial")).toHaveAttribute(
      "data-label-type",
      "label-tooltip",
    );
    expect(
      screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-speed-dial-person"),
    ).toBeInTheDocument();
    expect(
      screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-speed-dial-item"),
    ).toBeInTheDocument();
    expect(
      screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-speed-dial-cart"),
    ).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(/speed-dial/);
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(
      "icons-person-item-cart",
    );

    await user.click(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-speed-dial-labels"));
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-speed-dial")).toHaveAttribute(
      "data-label-type",
      "icon-only",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent("no-labels");

    const iconsRoot = screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-speed-dial-icons");
    await user.click(within(iconsRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-speed-dial-icons-field"));
    await user.click(
      within(iconsRoot).getByTestId("ui-standard-org-ehg-qa-btn-pg-speed-dial-icons-option-sparkles"),
    );
    expect(
      screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-preview-speed-dial-sparkles"),
    ).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-qa-btn-pg-copy")).toHaveTextContent(/sparkles/);
  });

  it("applies dark speed-dial label styles on QA glass panel hosts", () => {
    expect(css).toContain(
      '.org-ehg-qa-glass-panel[data-org-ehg-theme="dark"]\n  .org-ehg-btn-glass-speed-dial[data-label-type="label-tooltip"]\n  .org-ehg-btn-glass-speed-dial__label',
    );
    expect(css).toContain(
      '.org-ehg[data-org-ehg-theme="dark"]\n  .org-ehg-btn-glass-speed-dial[data-label-type="label-tooltip"]\n  .org-ehg-btn-glass-speed-dial__label',
    );
  });
});
