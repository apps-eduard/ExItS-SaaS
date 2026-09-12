import { describe, expect, it, vi, beforeEach } from "vitest";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { AdminManagementShell } from "@/features/admin/AdminManagementShell";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

const useWorkspaceMock = vi.fn();

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => useWorkspaceMock(),
}));

function renderShell(pathname = "/org") {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={[pathname]}>
            <AdminManagementShell>
              <div data-testid="admin-shell-child">Child</div>
            </AdminManagementShell>
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("AdminManagementShell", () => {
  beforeEach(() => {
    useWorkspaceMock.mockReturnValue({
      boundWorkspace: {
        organizationId: "11111111-1111-1111-1111-111111111111",
        organizationDisplayName: "Kizy Store",
        branchId: null,
        branchName: null,
        experience: "manage_business",
      },
      sessionGrant: {
        productAccessAllowed: true,
        mappedPosRoleCode: "Owner",
        productLocalRoleCode: "Owner",
        membershipRole: "OrganizationOwner",
        organizationManagementAuthority: true,
        featureCodes: ["store-area-management", "store-warehouse"],
        grantedFeatureCodes: [],
      },
    });
  });

  it("renders admin bottom nav with Home Manage Review More and no Sell", () => {
    renderShell("/org");
    expect(screen.getByTestId("admin-management-shell")).toBeInTheDocument();
    expect(screen.getByTestId("admin-mobile-nav")).toBeInTheDocument();
    expect(screen.getByTestId("admin-mobile-nav").className).toMatch(/\blg:hidden\b/);
    expect(screen.getByTestId("admin-mobile-header")).toBeInTheDocument();
    expect(screen.getByTestId("admin-mobile-header").className).toMatch(/\blg:hidden\b/);
    expect(screen.getByTestId("admin-mobile-home")).toBeInTheDocument();
    expect(screen.getByTestId("admin-mobile-manage")).toBeInTheDocument();
    expect(screen.getByTestId("admin-mobile-review")).toBeInTheDocument();
    expect(screen.getByTestId("admin-mobile-more")).toBeInTheDocument();
    expect(screen.queryByTestId("org-bottom-nav")).not.toBeInTheDocument();
    expect(screen.queryByTestId("org-nav-sell")).not.toBeInTheDocument();
    expect(screen.getByTestId("admin-shell-child")).toBeInTheDocument();
  });

  it("keeps desktop sidebar at lg+ and removes tablet rail", () => {
    renderShell("/org/branches");
    expect(screen.queryByTestId("admin-tablet-rail")).not.toBeInTheDocument();
    expect(screen.getByTestId("admin-desktop-sidebar")).toBeInTheDocument();
    expect(screen.getByTestId("admin-desktop-sidebar").className).toMatch(/\bhidden\b/);
    expect(screen.getByTestId("admin-desktop-sidebar").className).toMatch(/\blg:block\b/);
    expect(screen.getByTestId("admin-nav-branches")).toBeInTheDocument();
    expect(screen.getByTestId("admin-nav-areas")).toBeInTheDocument();
    expect(screen.getByTestId("admin-sidebar-switch-workspace")).toBeInTheDocument();
    expect(screen.getByTestId("admin-sidebar-switch-workspace")).toHaveAttribute("href", "/workspace");
    expect(screen.getByTestId("admin-management-shell").className).toContain("lg:pb-0");
    expect(screen.getByTestId("admin-management-shell").className).toMatch(/lg:flex-row/);
    expect(screen.getByTestId("admin-sidebar-brand")).toBeInTheDocument();
    expect(screen.getByTestId("admin-desktop-sidebar").className).toMatch(/\badmin-sidebar-rail\b/);
  });

  it("places desktop sidebar brand inside the full-height rail ahead of content", () => {
    renderShell("/org");
    const rail = screen.getByTestId("admin-desktop-sidebar");
    const brand = screen.getByTestId("admin-sidebar-brand");
    expect(rail.contains(brand)).toBe(true);
    expect(brand.querySelector(".admin-sidebar__mark")).toBeInTheDocument();
    expect(brand.querySelector(".admin-sidebar__brand-label")).toBeInTheDocument();
  });

  it("hides duplicate desktop topbar brand via shell composition CSS", () => {
    const globalsCss = readFileSync(
      resolve(dirname(fileURLToPath(import.meta.url)), "../../styles/globals.css"),
      "utf8",
    );
    expect(globalsCss).toMatch(
      /\.app-top-bar\.app-top-bar--shell-desktop\s+\.app-top-bar__brand\s*\{[^}]*display:\s*none/,
    );
  });

  it("does not inject XL Plan Usage on Overview; keeps it for /org/manage*", () => {
    const { unmount } = renderShell("/org");
    expect(screen.queryByTestId("admin-xl-context")).not.toBeInTheDocument();
    unmount();

    renderShell("/org/manage/staff");
    expect(screen.getByTestId("admin-xl-context")).toBeInTheDocument();
  });
});
