import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { OperationsShell } from "@/features/operations/OperationsShell";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      organizationDisplayName: "Test Org",
      branchId: "22222222-2222-2222-2222-222222222222",
      branchName: "Main",
      branchType: "Retail",
      experience: "operations",
    },
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "StoreManager",
    },
  }),
}));

vi.mock("@/features/operations/OperationsSidebar", () => ({
  OperationsSidebar: () => <div data-testid="operations-sidebar-stub" />,
}));

vi.mock("@/features/operations/OperationsBottomNav", () => ({
  OperationsBottomNav: () => <div data-testid="operations-bottom-nav-stub" />,
}));

describe("OperationsShell sell-floor viewport containment", () => {
  it("applies sell-floor class and min-h-0 content chain without body overflow lock", () => {
    const { rerender } = render(
      <MemoryRouter>
        <OperationsShell sellFloor>
          <div data-testid="sell-child">sell</div>
        </OperationsShell>
      </MemoryRouter>,
    );

    const shell = screen.getByTestId("operations-shell");
    expect(shell.className).toContain("operations-shell--sell-floor");
    expect(screen.queryByTestId("operations-mobile-header")).not.toBeInTheDocument();
    expect(document.getElementById("main-content")?.className).toMatch(/min-h-0/);
    expect(document.body.style.overflow).not.toBe("hidden");

    rerender(
      <MemoryRouter>
        <OperationsShell>
          <div data-testid="ops-child">ops</div>
        </OperationsShell>
      </MemoryRouter>,
    );

    expect(screen.getByTestId("operations-shell").className).not.toContain(
      "operations-shell--sell-floor",
    );
    expect(screen.getByTestId("operations-mobile-header")).toBeInTheDocument();
  });
});
