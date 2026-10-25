import type { ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgQuickActionGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgQuickActionGlassGallery";

function wrap(ui: ReactNode) {
  return (
    <MemoryRouter>
      <PreferencesProvider>
        <ToastProvider>{ui}</ToastProvider>
      </PreferencesProvider>
    </MemoryRouter>
  );
}

describe("OrgEhgQuickActionGlassGallery", () => {
  it("renders playground plus light and dark Quick Action glass samples", () => {
    render(wrap(<OrgEhgQuickActionGlassGallery />));
    expect(screen.getByTestId("ui-standard-org-ehg-quick-action-glass")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-org-ehg-qa-glass-playground")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-qa-glass-light")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-qa-glass-dark")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-qa-glass-light-primary")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-qa-glass-light-icon-soft")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-qa-glass-light-icon-rounded")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-qa-glass-light-size-sm")).toHaveClass(
      "org-ehg-qa-glass--size-sm",
    );
    expect(screen.getByTestId("org-ehg-qa-glass-light-size-md")).toHaveClass(
      "org-ehg-qa-glass--size-md",
    );
    expect(screen.getByTestId("org-ehg-qa-glass-light-size-lg")).toHaveClass(
      "org-ehg-qa-glass--size-lg",
    );
    expect(screen.getByTestId("org-ehg-qa-glass-light-badge")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-qa-glass-light-split")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-qa-glass-light-ghost")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-qa-glass-light-group")).toHaveClass("org-ehg-qa-glass");
    expect(
      within(screen.getByTestId("org-ehg-qa-glass-light-group")).getByTestId(
        "org-ehg-qa-glass-light-group-group",
      ),
    ).toHaveAttribute("data-icon-shape", "soft");
    expect(
      within(screen.getByTestId("org-ehg-qa-glass-light-group-rounded")).getByTestId(
        "org-ehg-qa-glass-light-group-rounded-group",
      ),
    ).toHaveAttribute("data-icon-shape", "rounded");
    expect(screen.getByTestId("org-ehg-qa-glass-light-action-icon")).toHaveClass("org-ehg-qa-glass");
    expect(
      within(screen.getByTestId("org-ehg-qa-glass-light-action-icon")).getByTestId(
        "org-ehg-qa-glass-light-action-icon-action-icon",
      ),
    ).toHaveClass("org-ehg-qa-glass-icon--action");
    expect(screen.getByTestId("org-ehg-qa-glass-light-icons")).toBeInTheDocument();
  });

  it("toggles the Dark Mode switch sample", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgQuickActionGlassGallery />));
    const sw = screen.getByTestId("org-ehg-qa-glass-light-toggle-switch");
    expect(sw).toHaveAttribute("aria-pressed", "false");
    await user.click(sw);
    expect(sw).toHaveAttribute("aria-pressed", "true");
  });
});
