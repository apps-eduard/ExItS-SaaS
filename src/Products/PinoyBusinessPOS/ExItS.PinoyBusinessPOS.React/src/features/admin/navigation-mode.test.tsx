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

describe("desktop navigation modes (Standard / Compact / Reveal)", () => {
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

  it("keeps Standard labels visible with icons, brand header, and aria-current", () => {
    writeUiPreferences({ ...defaultUiPreferences, navigationMode: "standard" });
    renderWithProviders(<AdminSidebar />, "/org");

    const overview = screen.getByTestId("admin-nav-overview");
    expect(overview).toHaveAttribute("aria-current", "page");
    expect(overview).toHaveAttribute("aria-label");
    expect(overview).not.toHaveAttribute("title");
    expect(overview.querySelector(".admin-sidebar__label")).toBeInTheDocument();
    expect(overview.querySelector(".admin-sidebar__icon")).toBeInTheDocument();
    expect(screen.getByTestId("admin-sidebar-brand")).toBeInTheDocument();
    expect(document.documentElement.dataset.navigationMode).toBe("standard");
  });

  it("Compact idle hides labels, keeps accessible names, and uses ExitsTooltip without auto-expand", () => {
    writeUiPreferences({ ...defaultUiPreferences, navigationMode: "compact" });
    renderWithProviders(<OperationsSidebar />, "/inventory");

    const inventory = screen.getByTestId("ops-sidebar-inventory");
    expect(inventory).toHaveAttribute("aria-current", "page");
    expect(inventory.getAttribute("aria-label")).toBeTruthy();
    expect(inventory).not.toHaveAttribute("title");
    expect(inventory.querySelector(".admin-sidebar__icon")).toBeInTheDocument();
    expect(inventory.querySelector(".admin-sidebar__label")).toBeInTheDocument();
    expect(screen.getByTestId("operations-sidebar-brand")).toBeInTheDocument();
    expect(document.documentElement.dataset.navigationMode).toBe("compact");

    expect(globalsCss).toMatch(
      /\[data-navigation-mode="compact"\][\s\S]*?\.admin-sidebar__label[\s\S]*?clip:\s*rect\(0,\s*0,\s*0,\s*0\)/,
    );
    expect(globalsCss).toMatch(/\.exits-tooltip/);
    expect(globalsCss).not.toMatch(
      /\[data-navigation-mode="compact"\]\s*\.admin-sidebar\.admin-sidebar--expanded:hover/,
    );
    expect(globalsCss).not.toMatch(
      /\[data-navigation-mode="compact"\]\s*\.admin-sidebar\.admin-sidebar--expanded:focus-within/,
    );
  });

  it("Reveal pushes shell width via shared token (not overlay absolute panel)", () => {
    expect(globalsCss).toMatch(/--exits-shell-sidebar-width:\s*15\.5rem/);
    expect(globalsCss).toMatch(
      /html\[data-navigation-mode="reveal"\][\s\S]*?--exits-shell-sidebar-width:\s*3\.75rem/,
    );
    expect(globalsCss).toMatch(
      /html\[data-navigation-mode="reveal"\]:has\(\.admin-sidebar:focus-within\)[\s\S]*?--exits-shell-sidebar-width:\s*15\.5rem/,
    );
    expect(globalsCss).toMatch(
      /html\[data-navigation-mode="reveal"\]:has\(\.admin-sidebar:hover\)[\s\S]*?--exits-shell-sidebar-width:\s*15\.5rem/,
    );
    expect(globalsCss).toMatch(/\.admin-sidebar-rail[\s\S]*?width:\s*var\(--exits-shell-sidebar-width\)/);
    expect(globalsCss).toMatch(/--exits-sidebar-reveal-duration:\s*280ms/);
    expect(globalsCss).toMatch(/--exits-sidebar-collapse-duration:\s*240ms/);
    expect(globalsCss).toMatch(/--exits-sidebar-collapse-grace:\s*140ms/);
    expect(globalsCss).toMatch(/cubic-bezier\(0\.2,\s*0,\s*0,\s*1\)/);

    // Push model: reveal sidebar stays in flow (relative), not absolute overlay.
    expect(globalsCss).toMatch(
      /\[data-navigation-mode="reveal"\]\s*\.admin-sidebar\.admin-sidebar--expanded\s*\{[^}]*position:\s*relative/,
    );
    expect(globalsCss).not.toMatch(
      /\[data-navigation-mode="reveal"\]\s*\.admin-sidebar\.admin-sidebar--expanded\s*\{[^}]*position:\s*absolute/,
    );

    expect(globalsCss).toMatch(
      /\[data-navigation-mode="reveal"\][\s\S]*?\.admin-sidebar__label[\s\S]*?opacity:\s*0/,
    );
    expect(globalsCss).toMatch(
      /\[data-navigation-mode="reveal"\][\s\S]*?:hover[\s\S]*?\.admin-sidebar__label[\s\S]*?opacity:\s*1|:focus-within[\s\S]*?\.admin-sidebar__label[\s\S]*?opacity:\s*1/,
    );
    expect(globalsCss).toMatch(/\[data-motion="reduced"\][\s\S]*?--exits-sidebar-reveal-duration:\s*0ms/);
  });

  it("does not change mobile bottom-nav architecture in CSS", () => {
    expect(globalsCss).not.toMatch(
      /\[data-navigation-mode="compact"\][\s\S]{0,200}operations-bottom-nav/,
    );
    expect(globalsCss).not.toMatch(
      /\[data-navigation-mode="reveal"\][\s\S]{0,200}admin-mobile-nav/,
    );
  });
});
