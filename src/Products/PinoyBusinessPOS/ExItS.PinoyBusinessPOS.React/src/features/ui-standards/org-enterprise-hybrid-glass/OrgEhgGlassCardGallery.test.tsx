import type { ReactNode } from "react";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { ToastProvider } from "@/components/exits/ToastProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { OrgEhgGlassCardGallery } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgGlassCardGallery";

function wrap(ui: ReactNode) {
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
  rootTestId: string,
  optionTestIdSuffix: string,
) {
  const root = screen.getByTestId(rootTestId);
  await user.click(within(root).getByTestId(`${rootTestId}-field`));
  await user.click(within(root).getByTestId(`${rootTestId}-option-${optionTestIdSuffix}`));
}

describe("OrgEhgGlassCardGallery", () => {
  it("renders Glass card playground with Sample select and light/dark split preview", () => {
    render(wrap(<OrgEhgGlassCardGallery />));
    const root = screen.getByTestId("ui-standard-org-ehg-glass-card");
    expect(
      within(root).getByTestId("ui-standard-card-org-ehg-glass-card-playground"),
    ).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-bg-option",
      "soft-vignette",
    );
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "kpi",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-sample")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-tab-card")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-tab-header")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-tab-footer")).toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-lane-light")).toHaveAttribute(
      "data-org-ehg-theme",
      "light",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-lane-dark")).toHaveAttribute(
      "data-org-ehg-theme",
      "dark",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-card")).toHaveAttribute(
      "data-sample",
      "kpi",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-card-dark")).toHaveAttribute(
      "data-sample",
      "kpi",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-copy")).toHaveTextContent(
      "ext org-ehg glass-card sample-kpi soft",
    );
  });

  it("switches samples and customizes Empty header/footer on both lanes", async () => {
    const user = userEvent.setup();
    render(wrap(<OrgEhgGlassCardGallery />));

    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "empty");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "empty",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-card")).toHaveAttribute(
      "data-sample",
      "empty",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-card")).toHaveAttribute(
      "data-header",
      "subtitle",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-footer")).toHaveAttribute(
      "data-footer",
      "actions",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-copy")).toHaveTextContent(
      "ext org-ehg glass-card sample-empty soft header-subtitle footer-actions",
    );

    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-show-header")).toBeChecked();
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-show-footer")).toBeChecked();

    await user.click(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-show-header"));
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-card")).toHaveAttribute(
      "data-show-header",
      "false",
    );
    expect(
      screen.queryByTestId("ui-standard-org-ehg-glass-card-pg-preview-header"),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-copy")).toHaveTextContent(
      "no-header",
    );

    await user.click(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-tab-header"));
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-header-enabled")).not.toBeChecked();
    await user.click(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-header-enabled"));
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-header")).toBeInTheDocument();

    await user.click(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-tab-footer"));
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-footer-enabled")).toBeChecked();
    await user.click(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-footer-enabled"));
    expect(
      screen.queryByTestId("ui-standard-org-ehg-glass-card-pg-preview-footer"),
    ).not.toBeInTheDocument();
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-copy")).toHaveTextContent(
      "no-footer",
    );

    await user.click(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-footer-enabled"));
    const footerRoot = screen.getByTestId("ui-standard-org-ehg-glass-card-pg-footer");
    await user.click(within(footerRoot).getByTestId("ui-standard-org-ehg-glass-card-pg-footer-field"));
    await user.click(
      within(footerRoot).getByTestId("ui-standard-org-ehg-glass-card-pg-footer-option-caption"),
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-footer")).toHaveAttribute(
      "data-footer",
      "caption",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-copy")).toHaveTextContent(
      "footer-caption",
    );

    await user.click(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-tab-card"));
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-fill", "hybrid-glass");
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-card")).toHaveAttribute(
      "data-fill",
      "hybrid",
    );
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-copy")).toHaveTextContent(
      "sample-empty hybrid",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-fill", "outline");
    expect(screen.getByTestId("ui-standard-org-ehg-glass-card-pg-preview-card")).toHaveAttribute(
      "data-fill",
      "outline",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-fill", "soft-glass");
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "hybrid-kpi");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "hybrid-kpi",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "solid-data");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "solid",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "orders-table");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "table",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "activity-timeline");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "timeline",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "setting-form");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "settings",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "edit-form");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "edit",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "confirm-action");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "confirm",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "profile");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "profile",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "checklist");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "checklist",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "notifications");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "notification",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "pricing-plan");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "pricing",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "invoice");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "invoice",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "staff-invite");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "invite",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "schedule-event");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "schedule",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "media-preview");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "media",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "branch-comparison");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "comparison",
    );
    await chooseDropdown(user, "ui-standard-org-ehg-glass-card-pg-sample", "loading-skeleton");
    expect(screen.getByTestId("ui-standard-card-org-ehg-glass-card-playground")).toHaveAttribute(
      "data-sample",
      "loading",
    );
  });
});
