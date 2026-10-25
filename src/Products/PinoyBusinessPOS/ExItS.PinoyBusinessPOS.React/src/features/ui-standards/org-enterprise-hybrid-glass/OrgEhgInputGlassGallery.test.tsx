import { describe, expect, it } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgInputGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgInputGlassGallery";

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

describe("OrgEhgInputGlassGallery", () => {
  it("renders playground above light/dark input glass panels", () => {
    render(wrap(<OrgEhgInputGlassGallery />));
    const root = screen.getByTestId("ui-standard-org-ehg-input-glass");
    const playground = within(root).getByTestId("ui-standard-card-org-ehg-input-glass-playground");
    const light = within(root).getByTestId("org-ehg-input-glass-light");
    expect(light).toHaveAttribute("data-org-ehg-theme", "light");
    expect(within(root).getByTestId("org-ehg-input-glass-dark")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );
    expect(playground.compareDocumentPosition(light) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(within(light).getByTestId("org-ehg-input-glass-light-soft")).toBeInTheDocument();
    expect(within(light).getByTestId("org-ehg-input-glass-light-select-caret")).toBeInTheDocument();
  });

  it("updates playground intensity and icon top", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgInputGlassGallery />));

    await chooseDropdown(user, "ui-standard-org-ehg-input-pg-intensity", "ultra-glass");
    expect(screen.getByTestId("ui-standard-org-ehg-input-pg-preview-field")).toHaveClass(
      "org-ehg-input-glass-field--ultra",
    );

    await chooseDropdown(user, "ui-standard-org-ehg-input-pg-icon-place", "top");
    expect(
      within(screen.getByTestId("ui-standard-org-ehg-input-pg-preview-field")).getByText("Email"),
    ).toBeInTheDocument();

    await user.click(screen.getByTestId("ui-standard-org-ehg-input-pg-dropdown"));
    expect(screen.getByTestId("ui-standard-org-ehg-input-pg-preview-field-caret")).toBeInTheDocument();

    await user.click(screen.getByTestId("ui-standard-org-ehg-input-pg-preview-field-trigger"));
    expect(await screen.findByTestId("ui-standard-org-ehg-input-pg-preview-field-menu")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-org-ehg-input-pg-preview-field-option-cash"));
    expect(screen.getByTestId("ui-standard-org-ehg-input-pg-preview-field-trigger")).toHaveTextContent(
      "Cash",
    );
  });

  it("applies playground background, theme, and field animations", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgInputGlassGallery />));

    expect(screen.getByTestId("org-ehg-input-glass-light-anim-bg-shimmer")).toHaveClass(
      "org-ehg-input-glass-field--bg-shimmer",
    );
    expect(screen.getByTestId("org-ehg-input-glass-light-anim-focus-ring")).toHaveClass(
      "org-ehg-input-glass-field--focus-ring-pulse",
    );
    expect(screen.getByTestId("org-ehg-input-glass-light-anim-focus-thick-below")).toHaveClass(
      "org-ehg-input-glass-field--focus-thick-below",
    );
    expect(
      within(screen.getByTestId("org-ehg-input-glass-light-anim-focus-thick-below")).getByText(
        "Thick below",
      ),
    ).toBeInTheDocument();

    const card = screen.getByTestId("ui-standard-card-org-ehg-input-glass-playground");
    expect(card).toHaveAttribute("data-bg-option", "soft-ambient");
    await chooseDropdown(user, "ui-standard-org-ehg-input-pg-background", "3.-soft-aurora-background");
    expect(card).toHaveAttribute("data-bg-option", "soft-aurora");
    await chooseDropdown(user, "ui-standard-org-ehg-input-pg-background", "6.-horizon-wash");
    expect(card).toHaveAttribute("data-bg-option", "horizon-wash");

    await user.click(screen.getByTestId("ui-standard-org-ehg-input-pg-theme"));
    expect(card).toHaveAttribute("data-org-ehg-theme", "dark");

    await chooseDropdown(user, "ui-standard-org-ehg-input-pg-focus-animation", "ring-pulse");
    expect(screen.getByTestId("ui-standard-org-ehg-input-pg-preview-field")).toHaveClass(
      "org-ehg-input-glass-field--focus-ring-pulse",
    );

    await chooseDropdown(user, "ui-standard-org-ehg-input-pg-focus-animation", "thick-below");
    expect(screen.getByTestId("ui-standard-org-ehg-input-pg-preview-field")).toHaveClass(
      "org-ehg-input-glass-field--focus-thick-below",
    );
    expect(
      screen
        .getByTestId("ui-standard-org-ehg-input-pg-preview-field")
        .querySelector(".org-ehg-input-glass-field__focus-bar"),
    ).not.toBeNull();
  });
});
