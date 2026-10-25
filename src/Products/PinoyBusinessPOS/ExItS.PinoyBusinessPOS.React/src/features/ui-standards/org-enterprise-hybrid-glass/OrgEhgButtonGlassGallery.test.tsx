import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { render, screen, within } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgButtonGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgButtonGlassGallery";

const css = readFileSync(
  resolve(__dirname, "org-enterprise-hybrid-glass.css"),
  "utf8",
);

function renderGallery() {
  return render(
    <MemoryRouter>
      <PreferencesProvider>
        <ToastProvider>
          <OrgEhgButtonGlassGallery />
        </ToastProvider>
      </PreferencesProvider>
    </MemoryRouter>,
  );
}

describe("OrgEhgButtonGlassGallery", () => {
  it("renders light and dark panels with rounded-rect glass intensities (not pills)", () => {
    renderGallery();

    expect(screen.getByTestId("ui-standard-org-ehg-button-glass")).toBeInTheDocument();
    const light = screen.getByTestId("org-ehg-btn-glass-light");
    const dark = screen.getByTestId("org-ehg-btn-glass-dark");
    expect(light).toHaveAttribute("data-org-ehg-theme", "light");
    expect(dark).toHaveAttribute("data-org-ehg-theme", "dark");
    expect(within(light).getByRole("heading", { name: /Light Mode – Button Glass Styles/i })).toBeInTheDocument();
    expect(within(light).getByText("Soft Glass")).toBeInTheDocument();
    expect(within(light).getByText("Main action")).toBeInTheDocument();
    expect(within(light).getByText("With Badge")).toBeInTheDocument();
    expect(within(light).getByText("XS")).toBeInTheDocument();
    expect(within(light).getByText("XL")).toBeInTheDocument();

    const soft = within(light).getByTestId("org-ehg-btn-glass-light-soft");
    expect(soft).toHaveClass("org-ehg-btn-glass--soft");
    expect(soft.className).not.toMatch(/rounded-full|pill/);
    expect(within(light).getByTestId("org-ehg-btn-glass-light-scene")).toBeInTheDocument();
    expect(within(light).getByTestId("org-ehg-btn-glass-light-split")).toBeInTheDocument();
  });

  it("locks MD height at 40px and XL at 56px for the size ladder", () => {
    expect(css).toContain("--org-ehg-btn-h-md: 2.5rem");
    expect(css).toContain("--org-ehg-btn-h-xl: 3.5rem");
    expect(css).toContain("--org-ehg-btn-h-xs: 1.75rem");
  });

  it("locks Org showcase radii to 8/10/12px and forbids pill radius on base button rule", () => {
    expect(css).toContain("--org-ehg-btn-r-sm: 8px");
    expect(css).toContain("--org-ehg-btn-r-md: 10px");
    expect(css).toContain("--org-ehg-btn-r-lg: 12px");
    expect(css).toContain("--org-ehg-btn-r-panel: 16px");
    expect(css).toContain("border-radius: var(--org-ehg-btn-glass-radius)");
    expect(css).toContain("backdrop-filter: blur(var(--org-ehg-btn-glass-blur))");
    const baseRule = css.match(/\.org-ehg-btn-glass\s*\{[^}]+\}/);
    expect(baseRule?.[0] ?? "").toContain("var(--org-ehg-btn-glass-radius)");
    expect(baseRule?.[0] ?? "").not.toMatch(/border-radius:\s*999/);
  });

  it("defines background and focus glass animation keyframes", () => {
    expect(css).toContain("@keyframes org-ehg-glass-bg-shimmer");
    expect(css).toContain("@keyframes org-ehg-glass-focus-ring-pulse");
    expect(css).toContain(".org-ehg-btn-glass--bg-shimmer");
    expect(css).toContain(".org-ehg-input-glass-field--focus-glow");
  });
});
