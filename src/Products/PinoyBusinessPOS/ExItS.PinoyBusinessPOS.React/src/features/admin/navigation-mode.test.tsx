import { beforeEach, describe, expect, it, vi } from "vitest";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminSidebar } from "@/features/admin/AdminSidebar";
import { OperationsSidebar } from "@/features/operations/OperationsSidebar";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import {
  defaultUiPreferences,
  UI_PREFERENCES_STORAGE_KEY,
  writeUiPreferences,
} from "@/lib/preferences/ui-preferences";
import { SIDEBAR_NAV_GROUPS_STORAGE_KEY } from "@/features/shell/sidebar-nav-group-accordion";

const rootDir = resolve(dirname(fileURLToPath(import.meta.url)), "../..");
const globalsCss = readFileSync(resolve(rootDir, "styles/globals.css"), "utf8");

vi.mock("@/features/purchasing/usePurchasingNavigationBadge", () => ({
  usePurchasingNavigationBadge: () => ({ count: 0, display: null }),
}));

const useWorkspaceMock = vi.fn();

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => useWorkspaceMock(),
}));

function renderWithProviders(ui: ReactNode, path: string) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={[path]}>{ui}</MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("desktop sidenav group accordion (Org Admin + Operations)", () => {
  beforeEach(() => {
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
    window.localStorage.removeItem(SIDEBAR_NAV_GROUPS_STORAGE_KEY);
    writeUiPreferences({ ...defaultUiPreferences, navigationMode: "standard" });
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        organizationDisplayName: "Kizy Store",
        branchId: "22222222-2222-2222-2222-222222222222",
        branchName: "Main",
        branchType: "Retail",
        experience: "operations",
      },
      workspaces: [],
      sessionGrant: {
        productAccessAllowed: true,
        mappedPosRoleCode: "Owner",
        productLocalRoleCode: "Owner",
        membershipRole: "OrganizationOwner",
        organizationManagementAuthority: true,
        canManageInventory: true,
        canViewReports: true,
        canCreateSale: true,
        featureCodes: ["store-area-management", "store-warehouse"],
        grantedFeatureCodes: [],
        capabilities: ["ViewPurchasing", "ViewInventory", "ViewOrders", "Sell"],
      },
    });
  });

  it("keeps full-width labels visible with icons, brand header, and aria-current", () => {
    renderWithProviders(<AdminSidebar />, "/org");

    const overview = screen.getByTestId("admin-nav-overview");
    expect(overview).toHaveAttribute("aria-current", "page");
    expect(overview).toHaveAttribute("aria-label");
    expect(overview.querySelector(".admin-sidebar__label")).toBeInTheDocument();
    expect(overview.querySelector(".admin-sidebar__icon")).toBeInTheDocument();
    expect(screen.getByTestId("admin-sidebar-brand")).toBeInTheDocument();
    expect(screen.getByTestId("admin-sidebar-brand-collapse-toggle")).toHaveAttribute(
      "aria-pressed",
      "true",
    );
    expect(screen.getByTestId("admin-sidebar-brand-collapse-toggle")).toHaveAttribute(
      "aria-label",
      "Collapse all",
    );
    // Brand toggle must not switch navigationMode to icon rail.
    expect(document.documentElement.dataset.navigationMode).toBe("standard");
  });

  it("collapses and expands an individual CONTROL group without changing sidebar width mode", async () => {
    const { default: userEvent } = await import("@testing-library/user-event");
    const user = userEvent.setup();
    renderWithProviders(<OperationsSidebar />, "/inventory");

    const stock = screen.getByTestId("sidebar-nav-group-stock");
    expect(stock).toHaveAttribute("data-expanded", "true");
    expect(screen.getByTestId("ops-sidebar-inventory")).toHaveAttribute("aria-current", "page");

    const controlToggle = screen.getByTestId("sidebar-nav-group-toggle-control");
    await user.click(controlToggle);
    expect(screen.getByTestId("sidebar-nav-group-control")).toHaveAttribute("data-expanded", "false");
    // Active STOCK group stays open and independent.
    expect(screen.getByTestId("sidebar-nav-group-stock")).toHaveAttribute("data-expanded", "true");
    expect(screen.getByTestId("ops-sidebar-inventory")).toBeVisible();
    expect(document.documentElement.dataset.navigationMode).toBe("standard");
  });

  it("brand Collapse all collapses non-active groups; Expand all restores them", async () => {
    const { default: userEvent } = await import("@testing-library/user-event");
    const user = userEvent.setup();
    renderWithProviders(<OperationsSidebar />, "/inventory");

    const brandToggle = screen.getByTestId("operations-sidebar-brand-collapse-toggle");
    expect(brandToggle).toHaveAttribute("aria-label", "Collapse all");

    await user.click(brandToggle);
    expect(brandToggle).toHaveAttribute("aria-label", "Expand all");
    expect(brandToggle).toHaveAttribute("aria-pressed", "false");
    // Active route group remains visible.
    expect(screen.getByTestId("sidebar-nav-group-stock")).toHaveAttribute("data-expanded", "true");
    expect(screen.getByTestId("ops-sidebar-inventory")).toHaveAttribute("aria-current", "page");
    expect(screen.getByTestId("sidebar-nav-group-control")).toHaveAttribute("data-expanded", "false");
    // Must not mutate navigationMode / icon-rail preference.
    expect(document.documentElement.dataset.navigationMode).toBe("standard");
    expect(JSON.parse(window.localStorage.getItem(UI_PREFERENCES_STORAGE_KEY) ?? "{}")).toMatchObject({
      navigationMode: "standard",
    });

    await user.click(brandToggle);
    expect(brandToggle).toHaveAttribute("aria-label", "Collapse all");
    expect(screen.getByTestId("sidebar-nav-group-control")).toHaveAttribute("data-expanded", "true");
  });

  it("persists group accordion preference across remount", async () => {
    const { default: userEvent } = await import("@testing-library/user-event");
    const user = userEvent.setup();
    const { unmount } = renderWithProviders(<OperationsSidebar />, "/inventory");

    await user.click(screen.getByTestId("sidebar-nav-group-toggle-control"));
    expect(screen.getByTestId("sidebar-nav-group-control")).toHaveAttribute("data-expanded", "false");
    unmount();

    renderWithProviders(<OperationsSidebar />, "/inventory");
    expect(screen.getByTestId("sidebar-nav-group-control")).toHaveAttribute("data-expanded", "false");
    expect(screen.getByTestId("sidebar-nav-group-stock")).toHaveAttribute("data-expanded", "true");
  });

  it("Org Admin multi-item group headers expose aria-expanded; Overview is a flat solo link", () => {
    renderWithProviders(<AdminSidebar />, "/org");
    expect(screen.getByTestId("admin-nav-overview")).toHaveAttribute("aria-current", "page");
    expect(screen.getByTestId("sidebar-nav-solo-overview")).toBeInTheDocument();
    expect(screen.queryByTestId("sidebar-nav-group-toggle-overview")).not.toBeInTheDocument();

    const organizationToggle = screen.getByTestId("sidebar-nav-group-toggle-organization");
    expect(organizationToggle.tagName).toBe("BUTTON");
    expect(organizationToggle).toHaveAttribute("aria-expanded", "true");
    expect(organizationToggle).toHaveAttribute("aria-controls", "sidebar-nav-group-panel-organization");
  });

  it("CSS still scopes Compact/Reveal to preference modes without brand-toggle rail wiring", () => {
    // Preference CSS may still exist; brand toggle must not be the rail switcher.
    expect(globalsCss).toMatch(/--exits-shell-sidebar-width:\s*15\.5rem/);
    expect(globalsCss).toMatch(/\.admin-sidebar__group-toggle/);
    expect(globalsCss).toMatch(/\.admin-sidebar__group-panel--open/);
    expect(globalsCss).toMatch(/grid-template-rows:\s*0fr/);
    expect(globalsCss).not.toMatch(
      /\[data-navigation-mode="compact"\][\s\S]{0,200}operations-bottom-nav/,
    );
    expect(globalsCss).not.toMatch(
      /\[data-navigation-mode="reveal"\][\s\S]{0,200}admin-mobile-nav/,
    );
  });
});
