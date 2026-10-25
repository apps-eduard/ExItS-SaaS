import { describe, expect, it } from "vitest";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgButtonGlassPlayground } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgButtonGlassPlayground";
import { OrgEhgButtonGlassGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgButtonGlassGallery";

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

describe("OrgEhgButtonGlassPlayground", () => {
  it("renders controls and a medium primary glass preview by default", () => {
    render(wrap(<OrgEhgButtonGlassPlayground />));
    expect(screen.getByTestId("ui-standard-card-org-ehg-btn-glass-playground")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-btn-glass-playground")).toBeInTheDocument();
    const previewBtn = screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-btn");
    expect(previewBtn).toHaveClass("org-ehg-btn-glass--medium");
    expect(previewBtn).toHaveClass("org-ehg-btn-glass--tone-primary");
    expect(previewBtn).toHaveClass("org-ehg-btn-glass--size-md");
    expect(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview")).toHaveAttribute(
      "data-org-ehg-theme",
      "light",
    );
  });

  it("updates intensity, size, and dark preview theme", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgButtonGlassPlayground />));

    await chooseDropdown(user, "ui-standard-org-ehg-btn-pg-intensity", "ultra-glass");
    expect(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-btn")).toHaveClass(
      "org-ehg-btn-glass--ultra",
    );

    await chooseDropdown(user, "ui-standard-org-ehg-btn-pg-size", "xl");
    expect(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-btn")).toHaveClass(
      "org-ehg-btn-glass--size-xl",
    );

    await user.click(screen.getByTestId("ui-standard-org-ehg-btn-pg-theme"));
    expect(screen.getByTestId("ui-standard-card-org-ehg-btn-glass-playground")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );
  });

  it("supports icon top placement and opens a dropdown menu", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgButtonGlassPlayground />));

    await chooseDropdown(user, "ui-standard-org-ehg-btn-pg-icon-place", "top");
    expect(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-btn")).toHaveClass(
      "org-ehg-btn-glass--icon-top",
    );

    await user.click(screen.getByTestId("ui-standard-org-ehg-btn-pg-dropdown"));
    expect(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-btn")).toHaveClass(
      "org-ehg-btn-glass--with-dropdown",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-caret")).toBeInTheDocument();

    await user.click(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-btn"));
    expect(await screen.findByTestId("ui-standard-org-ehg-btn-pg-preview-menu")).toBeInTheDocument();
    await user.click(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-option-save-draft"));
    expect(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-btn")).toHaveTextContent("Save draft");
  });

  it("sits above the light/dark gallery in Button glass", () => {
    render(wrap(<OrgEhgButtonGlassGallery />));
    const root = screen.getByTestId("ui-standard-org-ehg-button-glass");
    const playground = within(root).getByTestId("ui-standard-card-org-ehg-btn-glass-playground");
    const light = within(root).getByTestId("org-ehg-btn-glass-light");
    expect(playground.compareDocumentPosition(light) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
  });

  it("applies playground background and button animation classes", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgButtonGlassGallery />));

    expect(screen.getByTestId("org-ehg-btn-glass-light-anim-bg-shimmer")).toHaveClass(
      "org-ehg-btn-glass--bg-shimmer",
    );
    expect(screen.getByTestId("org-ehg-btn-glass-light-anim-focus-ring")).toHaveClass(
      "org-ehg-btn-glass--focus-ring-pulse",
    );

    const card = screen.getByTestId("ui-standard-card-org-ehg-btn-glass-playground");
    expect(card).toHaveAttribute("data-bg-option", "soft-ambient");
    await chooseDropdown(user, "ui-standard-org-ehg-btn-pg-background", "2.-subtle-mesh-gradient");
    expect(card).toHaveAttribute("data-bg-option", "subtle-mesh");
    await chooseDropdown(user, "ui-standard-org-ehg-btn-pg-background", "5.-soft-vignette");
    expect(card).toHaveAttribute("data-bg-option", "soft-vignette");
    await chooseDropdown(user, "ui-standard-org-ehg-btn-pg-background", "7.-spotlight-glow");
    expect(card).toHaveAttribute("data-bg-option", "spotlight-glow");

    await chooseDropdown(user, "ui-standard-org-ehg-btn-pg-focus-animation", "glow");
    expect(screen.getByTestId("ui-standard-org-ehg-btn-pg-preview-btn")).toHaveClass(
      "org-ehg-btn-glass--focus-glow",
    );
  });
});
