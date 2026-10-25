import { describe, expect, it } from "vitest";
import { fireEvent, render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgAppearanceLab } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgAppearanceLab";

function wrap(ui: React.ReactNode) {
  return (
    <MemoryRouter>
      <PreferencesProvider>
        <ToastProvider>{ui}</ToastProvider>
      </PreferencesProvider>
    </MemoryRouter>
  );
}

async function chooseBackground(
  user: ReturnType<typeof userEvent.setup>,
  optionSlug: string,
) {
  const root = screen.getByTestId("org-ehg-bg-lab-background");
  const field = within(root).getByTestId("org-ehg-bg-lab-background-field");
  if (field.getAttribute("aria-expanded") !== "true") {
    await user.click(field);
  }
  await user.click(within(root).getByTestId(`org-ehg-bg-lab-background-option-${optionSlug}`));
}

describe("OrgEhgAppearanceLab background playground", () => {
  it("renders Background select and seven option rows with Soft Ambient as default", () => {
    render(wrap(<OrgEhgAppearanceLab />));
    const root = screen.getByTestId("org-ehg-appearance");
    expect(within(root).getByTestId("org-ehg-bg-lab-background")).toBeInTheDocument();
    expect(within(root).getByTestId("org-ehg-bg-lab-options")).toBeInTheDocument();

    for (const id of [
      "soft-ambient",
      "subtle-mesh",
      "soft-aurora",
      "neutral-solid",
      "soft-vignette",
      "horizon-wash",
      "spotlight-glow",
    ] as const) {
      expect(within(root).getByTestId(`org-ehg-bg-lab-option-${id}`)).toBeInTheDocument();
    }

    expect(screen.getByTestId("org-ehg-bg-lab-option-soft-ambient")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute(
      "data-bg-option",
      "soft-ambient",
    );
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute(
      "data-org-ehg-theme",
      "light",
    );
    expect(screen.getByTestId("org-ehg-bg-lab-preview-kpi")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-bg-lab-preview-solid-card")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-bg-lab-preview-input")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-bg-lab-preview-primary-btn")).toBeInTheDocument();
  });

  it("switches option via select and rows, theme, intensity, glass, reset", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgAppearanceLab />));

    await chooseBackground(user, "2.-subtle-mesh-gradient");
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute(
      "data-bg-option",
      "subtle-mesh",
    );
    expect(screen.getByTestId("org-ehg-bg-lab-option-subtle-mesh")).toHaveAttribute(
      "aria-checked",
      "true",
    );
    expect(screen.getByTestId("org-ehg-bg-lab-selected-name")).toHaveTextContent(
      "2. Subtle Mesh Gradient",
    );

    await user.click(screen.getByTestId("org-ehg-bg-lab-theme-dark"));
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );

    const intensity = screen.getByTestId("org-ehg-bg-lab-intensity");
    fireEvent.change(intensity, { target: { value: "40" } });
    expect(intensity).toHaveValue("40");
    expect(screen.getByTestId("org-ehg-bg-lab-preview").getAttribute("style") ?? "").toMatch(
      /--org-ehg-bg-intensity:\s*0\.4/,
    );

    await user.click(screen.getByTestId("org-ehg-bg-lab-show-glass"));
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute("data-show-glass", "false");

    await user.click(screen.getByTestId("org-ehg-bg-lab-option-soft-aurora"));
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute(
      "data-bg-option",
      "soft-aurora",
    );

    await user.click(screen.getByTestId("org-ehg-bg-lab-option-neutral-solid"));
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute(
      "data-bg-option",
      "neutral-solid",
    );
    expect(screen.getByTestId("org-ehg-bg-lab-intensity")).toBeDisabled();

    await user.click(screen.getByTestId("org-ehg-bg-lab-reset"));
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute(
      "data-bg-option",
      "soft-ambient",
    );
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute(
      "data-org-ehg-theme",
      "light",
    );
    expect(screen.getByTestId("org-ehg-bg-lab-preview")).toHaveAttribute("data-show-glass", "true");
    expect(screen.getByTestId("org-ehg-bg-lab-intensity")).toHaveValue("15");
  });

  it("supports side-by-side light and dark previews", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgAppearanceLab />));

    await user.click(screen.getByTestId("org-ehg-bg-lab-side-by-side"));
    expect(screen.getByTestId("org-ehg-light-dark")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-bg-lab-preview-light")).toHaveAttribute(
      "data-org-ehg-theme",
      "light",
    );
    expect(screen.getByTestId("org-ehg-bg-lab-preview-dark")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );
  });
});
