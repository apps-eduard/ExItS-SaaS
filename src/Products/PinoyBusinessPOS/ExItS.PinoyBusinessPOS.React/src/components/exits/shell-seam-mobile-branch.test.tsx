import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AdminManagementShell } from "@/features/admin/AdminManagementShell";
import { OperationsShell } from "@/features/operations/OperationsShell";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "org",
      organizationDisplayName: "Org",
      branchId: "branch",
      branchName: "Main Branch",
      branchType: "Retail",
    },
    workspaces: [],
    clearBoundWorkspace: vi.fn(),
  }),
}));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    signOut: vi.fn(),
    session: { accountClass: "Organization", organizationContextLocked: false, displayName: "User" },
    status: "authenticated",
  }),
}));

vi.mock("@/session/account-class", () => ({
  isOrganizationContextLocked: () => false,
  sessionAccountClass: () => "Organization",
}));

vi.mock("@/features/operations/OperationsSidebar", () => ({
  OperationsSidebar: () => <aside data-testid="ops-sidebar-stub" />,
}));

vi.mock("@/features/operations/OperationsBottomNav", () => ({
  OperationsBottomNav: () => <nav data-testid="ops-bottom-nav-stub" />,
}));

vi.mock("@/features/admin/AdminSidebar", () => ({
  AdminSidebar: () => <aside data-testid="admin-sidebar-stub" />,
}));

vi.mock("@/features/admin/AdminMobileNav", () => ({
  AdminMobileNav: () => <nav data-testid="admin-bottom-nav-stub" />,
}));

vi.mock("@/features/admin/AdminContextPanel", () => ({
  AdminContextPanel: () => null,
}));

const opsShellSrc = readFileSync(
  resolve(__dirname, "../../features/operations/OperationsShell.tsx"),
  "utf8",
);
const adminShellSrc = readFileSync(
  resolve(__dirname, "../../features/admin/AdminManagementShell.tsx"),
  "utf8",
);
const globalsCss = readFileSync(resolve(__dirname, "../../styles/globals.css"), "utf8");
const topBarSrc = readFileSync(resolve(__dirname, "./AppTopBar.tsx"), "utf8");

describe("Shell seam + mobile branch polish", () => {
  it("removes desktop content-column start gutter so sidebar touches topbar/content", () => {
    expect(opsShellSrc).not.toMatch(/lg:ps-\[max\(0\.75rem/);
    expect(adminShellSrc).not.toMatch(/lg:ps-\[max\(0\.75rem/);
    expect(opsShellSrc).toMatch(/lg:pe-\[max\(var\(--exits-page-padding\)/);
    expect(adminShellSrc).toMatch(/lg:pe-\[max\(var\(--exits-page-padding\)/);
    expect(opsShellSrc).toMatch(/lg:gap-0/);
    expect(globalsCss).toMatch(
      /\.admin-sidebar[\s\S]*?border-inline-end:\s*1px solid/,
    );
    expect(globalsCss).toContain("--exits-shell-sidebar-width");
  });

  it("keeps Operations/Admin shell gap-0 at lg and mounts sidebar rail + column", () => {
    render(
      <MemoryRouter>
        <OperationsShell header={<div data-testid="ops-header">H</div>}>
          <div>content</div>
        </OperationsShell>
      </MemoryRouter>,
    );
    const shell = screen.getByTestId("operations-shell");
    expect(shell.className).toMatch(/lg:gap-0/);
    expect(screen.getByTestId("operations-desktop-sidebar").className).toMatch(/admin-sidebar-rail/);
    expect(shell.querySelector(".operations-shell__column")?.className).not.toMatch(/lg:ps-/);
  });

  it("hides branch selector below lg and preserves desktop workspace control", () => {
    expect(topBarSrc).not.toContain("workspace-context-mobile");
    expect(topBarSrc).toMatch(/app-top-bar__center hidden lg:flex/);
    expect(topBarSrc).toMatch(/Account menu|workspace\.switch|MOBILE|app\.name/);
  });

  it("Admin shell also has no start gutter", () => {
    render(
      <MemoryRouter>
        <AdminManagementShell header={<div>H</div>}>
          <div>content</div>
        </AdminManagementShell>
      </MemoryRouter>,
    );
    expect(screen.getByTestId("admin-management-shell").querySelector(".admin-shell__column")?.className).not.toMatch(
      /lg:ps-/,
    );
  });
});
