import type { ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgFabGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgFabGlassGallery";

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
  it("renders floating action playground and light/dark FAB samples", () => {
    render(wrap(<OrgEhgFabGlassGallery />));
    expect(screen.getByTestId("ui-standard-org-ehg-fab-glass")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-org-ehg-btn-glass-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-btn")).toHaveClass(
      "org-ehg-btn-glass--fab",
    );
    const light = screen.getByTestId("org-ehg-fab-glass-light");
    expect(within(light).getByTestId("org-ehg-fab-glass-light-fab")).toHaveClass(
      "org-ehg-btn-glass--fab",
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
});
