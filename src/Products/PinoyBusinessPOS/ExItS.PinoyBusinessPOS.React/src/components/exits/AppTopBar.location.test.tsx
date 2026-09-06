import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { AppTopBar } from "@/components/exits/AppTopBar";
import type { BoundWorkspace } from "@/workspace/types";
import { TEST_ORG_A_ID } from "@/test/session-context";

const WH_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const RETAIL_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

const navState = {
  bound: null as BoundWorkspace | null,
  workspaces: [] as Array<{
    organizationId: string;
    displayName: string;
    branches: Array<{
      branchId: string;
      name: string;
      secondaryLine: string;
      isPrimary: boolean;
      isActive: boolean;
      areaId?: string | null;
      areaName?: string | null;
      branchType?: "Retail" | "Warehouse";
    }>;
  }>,
  locked: false,
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: navState.bound,
    workspaces: navState.workspaces,
    clearBoundWorkspace: vi.fn(),
  }),
}));

vi.mock("@/session/SessionProvider", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/session/SessionProvider")>();
  return {
    ...actual,
    useSession: () => ({
      signOut: vi.fn(async () => ({ ok: true, nextRoute: "/sign-in" })),
      session: {
        accountClass: "Organization",
        organizationContextLocked: navState.locked,
        displayName: "Owner",
      },
      status: "authenticated",
    }),
  };
});

vi.mock("@/session/account-class", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/session/account-class")>();
  return {
    ...actual,
    isOrganizationContextLocked: () => navState.locked,
    sessionAccountClass: () => "Organization",
  };
});

function LocationProbe() {
  const location = useLocation();
  return <div data-testid="path-probe">{location.pathname}</div>;
}

function renderTopBar(initialPath = "/warehouse") {
  return render(
    <AppProviders>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route
            path="*"
            element={
              <>
                <AppTopBar />
                <LocationProbe />
              </>
            }
          />
        </Routes>
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("AppTopBar location clarity", () => {
  beforeEach(() => {
    navState.locked = false;
    navState.workspaces = [
      {
        organizationId: TEST_ORG_A_ID,
        displayName: "Kizy Store",
        branches: [
          {
            branchId: WH_ID,
            name: "Panay Warehouse",
            secondaryLine: "Active",
            isPrimary: false,
            isActive: true,
            areaId: "area-1",
            areaName: "Pacifica Nort Area",
            branchType: "Warehouse",
          },
          {
            branchId: RETAIL_ID,
            name: "Main Branch",
            secondaryLine: "Active",
            isPrimary: true,
            isActive: true,
            areaId: "area-2",
            areaName: "Pasi Norte",
            branchType: "Retail",
          },
        ],
      },
    ];
    navState.bound = {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: WH_ID,
      branchName: "Panay Warehouse",
      branchType: "Warehouse",
      areaId: "area-1",
      areaName: "Pacifica Nort Area",
      experience: "operations",
    };
  });

  it("renders warehouse location primary and area/type secondary", () => {
    renderTopBar();
    expect(screen.getByTestId("workspace-context-primary")).toHaveTextContent("Panay Warehouse");
    expect(screen.getByTestId("workspace-context-secondary")).toHaveTextContent(
      "Pacifica Nort Area · Warehouse",
    );
    expect(screen.getByTestId("workspace-context")).toHaveAttribute("data-location-type", "Warehouse");
    expect(screen.getByTestId("workspace-context")).not.toHaveTextContent(/^Pacifica Nort Area$/);
  });

  it("renders retail location with area secondary", () => {
    navState.bound = {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: RETAIL_ID,
      branchName: "Main Branch",
      branchType: "Retail",
      areaId: "area-2",
      areaName: "Pasi Norte",
      experience: "operations",
    };
    renderTopBar("/role/manager");
    expect(screen.getByTestId("workspace-context-primary")).toHaveTextContent("Main Branch");
    expect(screen.getByTestId("workspace-context-secondary")).toHaveTextContent("Pasi Norte · Retail");
  });

  it("renders type only when no area", () => {
    navState.bound = {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: RETAIL_ID,
      branchName: "Main Branch",
      branchType: "Retail",
      areaId: null,
      areaName: null,
      experience: "start_selling",
    };
    renderTopBar("/sell");
    expect(screen.getByTestId("workspace-context-primary")).toHaveTextContent("Main Branch");
    expect(screen.getByTestId("workspace-context-secondary")).toHaveTextContent("Retail");
  });

  it("navigates whole selector to /workspace and no-ops when already there", async () => {
    const user = userEvent.setup();
    renderTopBar("/inventory");
    await user.click(screen.getByTestId("workspace-context"));
    expect(screen.getByTestId("path-probe")).toHaveTextContent("/workspace");

    await user.click(screen.getByTestId("workspace-context"));
    expect(screen.getByTestId("path-probe")).toHaveTextContent("/workspace");
  });

  it("shows choose workspace when unbound and keeps chevron when switchable", () => {
    navState.bound = null;
    renderTopBar("/org");
    expect(screen.getByTestId("workspace-context-primary")).toHaveTextContent("Choose workspace");
    expect(screen.getByTestId("workspace-context")).not.toBeDisabled();
  });

  it("hides chevron and disables when workspace switching is locked", () => {
    navState.locked = true;
    renderTopBar();
    expect(screen.getByTestId("workspace-context")).toBeDisabled();
    expect(screen.getByTestId("workspace-context").querySelector(".app-top-bar__workspace-chevron")).toBeNull();
  });

  it("exposes accessible change-workspace label with location details", () => {
    renderTopBar();
    expect(screen.getByTestId("workspace-context")).toHaveAttribute(
      "aria-label",
      expect.stringContaining("Panay Warehouse"),
    );
    expect(screen.getByTestId("workspace-context")).toHaveAttribute(
      "title",
      expect.stringContaining("Panay Warehouse"),
    );
  });

  it("truncates long names via title without pushing primary away", () => {
    navState.bound = {
      organizationId: TEST_ORG_A_ID,
      organizationDisplayName: "Kizy Store",
      branchId: WH_ID,
      branchName: "North Western Regional Distribution Warehouse",
      branchType: "Warehouse",
      areaId: "area-1",
      areaName: "Western Visayas Regional Operations Area",
      experience: "operations",
    };
    renderTopBar();
    expect(screen.getByTestId("workspace-context-primary")).toHaveTextContent(
      "North Western Regional Distribution Warehouse",
    );
    expect(screen.getByTestId("workspace-context")).toHaveAttribute(
      "title",
      expect.stringContaining("Western Visayas Regional Operations Area"),
    );
  });
});
