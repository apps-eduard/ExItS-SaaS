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

describe("desktop navigation mode (Standard / Compact)", () => {
  beforeEach(() => {
    window.localStorage.removeItem(UI_PREFERENCES_STORAGE_KEY);
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        organizationDisplayName: "Kizy Store",
        branchId: "22222222-2222-2222-2222-222222222222",
        branchName: "Main",
        branchType: "Retail",
        experience: "operations",
      },
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

  it("keeps Standard labels visible with icons and aria-current", () => {
    writeUiPreferences({ ...defaultUiPreferences, navigationMode: "standard" });
    renderWithProviders(<AdminSidebar />, "/org");

    const overview = screen.getByTestId("admin-nav-overview");
    expect(overview).toHaveAttribute("aria-current", "page");
    expect(overview).not.toHaveAttribute("title");
    expect(overview).toHaveAttribute("aria-label");
    expect(overview.querySelector(".admin-sidebar__label")).toBeInTheDocument();
    expect(overview.querySelector(".admin-sidebar__icon")).toBeInTheDocument();
    expect(document.documentElement.dataset.navigationMode).toBe("standard");
  });

  it("Compact idle hides labels visually while preserving accessible names", () => {
    writeUiPreferences({ ...defaultUiPreferences, navigationMode: "compact" });
    renderWithProviders(<OperationsSidebar />, "/inventory");

    const inventory = screen.getByTestId("ops-sidebar-inventory");
    expect(inventory).toHaveAttribute("aria-current", "page");
    expect(inventory).not.toHaveAttribute("title");
    expect(inventory.getAttribute("aria-label")).toBeTruthy();
    expect(inventory.querySelector(".admin-sidebar__icon")).toBeInTheDocument();
    expect(inventory.querySelector(".admin-sidebar__label")).toBeInTheDocument();
    expect(document.documentElement.dataset.navigationMode).toBe("compact");

    expect(globalsCss).toMatch(
      /\[data-navigation-mode="compact"\][\s\S]*?\.admin-sidebar__label[\s\S]*?clip:\s*rect\(0,\s*0,\s*0,\s*0\)/,
    );
  });

  it("Compact reveal expands over content on hover and focus-within without resizing the rail", () => {
    expect(globalsCss).toMatch(/\.admin-sidebar-rail/);
    expect(globalsCss).toMatch(
      /\[data-navigation-mode="compact"\][\s\S]*?\.admin-sidebar-rail[\s\S]*?width:\s*3\.75rem/,
    );
    expect(globalsCss).toMatch(
      /\[data-navigation-mode="compact"\][\s\S]*?\.admin-sidebar\.admin-sidebar--expanded:hover/,
    );
    expect(globalsCss).toMatch(
      /\[data-navigation-mode="compact"\][\s\S]*?\.admin-sidebar\.admin-sidebar--expanded:focus-within/,
    );
    expect(globalsCss).toMatch(
      /:hover[\s\S]*?width:\s*15\.5rem[\s\S]*?:focus-within|:focus-within[\s\S]*?width:\s*15\.5rem/,
    );
    expect(globalsCss).toMatch(
      /:hover[\s\S]*?\.admin-sidebar__label[\s\S]*?position:\s*static|:focus-within[\s\S]*?\.admin-sidebar__label[\s\S]*?position:\s*static/,
    );
    expect(globalsCss).toMatch(/inset-inline-start:\s*0/);
    expect(globalsCss).toMatch(/box-shadow:\s*var\(--exits-shadow-md\)/);
    // Motion tokens drive reveal duration (0 under reduced motion).
    expect(globalsCss).toMatch(/width var\(--exits-motion-normal\)/);
  });

  it("does not change mobile bottom-nav architecture in CSS", () => {
    expect(globalsCss).not.toMatch(
      /\[data-navigation-mode="compact"\][\s\S]{0,200}operations-bottom-nav/,
    );
    expect(globalsCss).not.toMatch(
      /\[data-navigation-mode="compact"\][\s\S]{0,200}admin-mobile-nav/,
    );
  });
});
