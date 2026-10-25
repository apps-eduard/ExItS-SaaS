import { describe, expect, it } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgMenuGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgMenuGlassGallery";

function wrap(ui: React.ReactNode) {
  return (
    <MemoryRouter>
      <PreferencesProvider>
        <ToastProvider>{ui}</ToastProvider>
      </PreferencesProvider>
    </MemoryRouter>
  );
}

async function chooseDropdown(
  user: ReturnType<typeof userEvent.setup>,
  testId: string,
  optionSlug: string,
) {
  const root = screen.getByTestId(testId);
  const field = within(root).getByTestId(`${testId}-field`);
  if (field.getAttribute("aria-expanded") !== "true") {
    await user.click(field);
  }
  await user.click(within(root).getByTestId(`${testId}-option-${optionSlug}`));
}

describe("OrgEhgMenuGlassGallery", () => {
  it("renders playground above light/dark menu glass panels with useful presets", () => {
    render(wrap(<OrgEhgMenuGlassGallery />));
    const root = screen.getByTestId("ui-standard-org-ehg-menu-glass");
    const playground = within(root).getByTestId("ui-standard-card-org-ehg-menu-glass-playground");
    const light = within(root).getByTestId("org-ehg-menu-glass-light");
    expect(light).toHaveAttribute("data-org-ehg-theme", "light");
    expect(within(root).getByTestId("org-ehg-menu-glass-dark")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );
    expect(playground.compareDocumentPosition(light) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(light).getByTestId("org-ehg-menu-glass-light-workspace")).toBeInTheDocument();
    expect(within(light).getByTestId("org-ehg-menu-glass-light-account")).toBeInTheDocument();
    expect(within(light).getByTestId("org-ehg-menu-glass-light-quick-actions")).toBeInTheDocument();
    expect(within(light).getByTestId("org-ehg-menu-glass-light-medium")).toBeInTheDocument();
    expect(
      within(within(light).getByTestId("org-ehg-menu-glass-light-workspace")).getByText(
        "ExItS Retail Coop",
      ),
    ).toBeInTheDocument();
    expect(
      within(within(light).getByTestId("org-ehg-menu-glass-light-account")).getByText("Maria Santos"),
    ).toBeInTheDocument();
  });

  it("updates preset, intensity, density, background, theme, and selection", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgMenuGlassGallery />));

    const card = screen.getByTestId("ui-standard-card-org-ehg-menu-glass-playground");
    expect(card).toHaveAttribute("data-bg-option", "soft-ambient");

    const preview = screen.getByTestId("ui-standard-org-ehg-menu-pg-preview-card-surface");
    expect(preview).toHaveClass("org-ehg-menu-glass--preset-workspace");
    expect(preview).toHaveClass("org-ehg-menu-glass--comfortable");

    await chooseDropdown(user, "ui-standard-org-ehg-menu-pg-preset", "account-menu");
    expect(preview).toHaveClass("org-ehg-menu-glass--preset-account");
    expect(
      screen.getByTestId("ui-standard-org-ehg-menu-pg-preview-card-surface-item-profile"),
    ).toBeInTheDocument();

    await chooseDropdown(user, "ui-standard-org-ehg-menu-pg-intensity", "ultra-glass");
    expect(preview).toHaveClass("org-ehg-menu-glass--ultra");

    await chooseDropdown(user, "ui-standard-org-ehg-menu-pg-density", "compact");
    expect(preview).toHaveClass("org-ehg-menu-glass--compact");

    await chooseDropdown(user, "ui-standard-org-ehg-menu-pg-background", "5.-soft-vignette");
    expect(card).toHaveAttribute("data-bg-option", "soft-vignette");

    await user.click(screen.getByTestId("ui-standard-org-ehg-menu-pg-theme"));
    expect(card).toHaveAttribute("data-org-ehg-theme", "dark");

    await user.click(
      screen.getByTestId("ui-standard-org-ehg-menu-pg-preview-card-surface-item-alerts"),
    );
    expect(
      screen.getByTestId("ui-standard-org-ehg-menu-pg-preview-card-surface-item-alerts"),
    ).toHaveAttribute("aria-current", "page");
  });
});
