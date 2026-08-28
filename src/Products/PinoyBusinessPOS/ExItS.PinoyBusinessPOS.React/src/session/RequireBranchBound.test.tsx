import { describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import { createMemoryRouter, MemoryRouter, RouterProvider } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { RequireBranchBound } from "@/session/SessionGuards";

vi.mock("@/session/SessionProvider", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/session/SessionProvider")>();
  return {
    ...actual,
    useSession: () => ({
      status: "authenticated",
      session: { userId: "u1", displayName: "Owner", accountClass: "Organization" },
    }),
  };
});

const bindDestination = vi.fn(async () => true);

let workspaceMock: {
  status: string;
  boundWorkspace: {
    organizationId: string;
    organizationDisplayName: string;
    branchId: string | null;
    branchName: string | null;
    experience: "manage_business" | "operations" | "start_selling";
  } | null;
  routingPlan: null;
  workspaces: Array<{
    organizationId: string;
    displayName: string;
    branches: Array<{ branchId: string; name: string }>;
  }>;
  bindDestination: typeof bindDestination;
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

function renderWithBranchGate(initialPath = "/catalog") {
  const router = createMemoryRouter(
    [
      {
        path: "/",
        children: [
          {
            path: "catalog",
            element: (
              <RequireBranchBound>
                <div data-testid="catalog-ok">catalog</div>
              </RequireBranchBound>
            ),
          },
          {
            path: "sell",
            element: (
              <RequireBranchBound>
                <div data-testid="sell-ok">sell</div>
              </RequireBranchBound>
            ),
          },
          {
            path: "inventory",
            element: (
              <RequireBranchBound>
                <div data-testid="inventory-ok">inventory</div>
              </RequireBranchBound>
            ),
          },
          {
            path: "org/branches",
            element: <div data-testid="branches-page">branches</div>,
          },
        ],
      },
    ],
    { initialEntries: [initialPath] },
  );

  render(
    <AppProviders>
      <RouterProvider router={router} />
    </AppProviders>,
  );

  return router;
}

describe("RequireBranchBound", () => {
  it("redirects to branch setup when Manage Business has no store branch", async () => {
    workspaceMock = {
      status: "bound",
      boundWorkspace: {
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationDisplayName: "Mica Org",
        branchId: null,
        branchName: null,
        experience: "manage_business",
      },
      routingPlan: null,
      workspaces: [
        {
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          displayName: "Mica Org",
          branches: [],
        },
      ],
      bindDestination,
    };

    renderWithBranchGate();

    expect(await screen.findByTestId("branches-page")).toBeInTheDocument();
    expect(screen.queryByTestId("branch-required-panel")).not.toBeInTheDocument();
    expect(bindDestination).not.toHaveBeenCalled();
  });

  it("auto-binds the only Main Branch when Sell is opened from Manage Business", async () => {
    bindDestination.mockClear();
    workspaceMock = {
      status: "bound",
      boundWorkspace: {
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationDisplayName: "Mica Org",
        branchId: null,
        branchName: null,
        experience: "manage_business",
      },
      routingPlan: null,
      workspaces: [
        {
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          displayName: "Mica Org",
          branches: [{ branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", name: "Main" }],
        },
      ],
      bindDestination,
    };

    renderWithBranchGate("/sell");

    expect(screen.queryByTestId("branch-required-panel")).not.toBeInTheDocument();
    await waitFor(() => {
      expect(bindDestination).toHaveBeenCalledWith(
        expect.objectContaining({
          branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
          experience: "start_selling",
          route: "/sell",
        }),
      );
    });
  });

  it("shows branch-required panel when multiple branches exist under org-only bind", () => {
    bindDestination.mockClear();
    workspaceMock = {
      status: "bound",
      boundWorkspace: {
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationDisplayName: "Mica Org",
        branchId: null,
        branchName: null,
        experience: "manage_business",
      },
      routingPlan: null,
      workspaces: [
        {
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          displayName: "Mica Org",
          branches: [
            { branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", name: "Main" },
            { branchId: "cccccccc-cccc-cccc-cccc-cccccccccccc", name: "Annex" },
          ],
        },
      ],
      bindDestination,
    };

    render(
      <AppProviders>
        <MemoryRouter>
          <RequireBranchBound>
            <div data-testid="catalog-ok">catalog</div>
          </RequireBranchBound>
        </MemoryRouter>
      </AppProviders>,
    );

    expect(screen.getByTestId("branch-required-panel")).toBeInTheDocument();
    expect(screen.getByTestId("branch-required-choose-workspace")).toBeInTheDocument();
    expect(bindDestination).not.toHaveBeenCalled();
  });

  it("renders children when a branch is bound", () => {
    workspaceMock = {
      status: "bound",
      boundWorkspace: {
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationDisplayName: "Mica Org",
        branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        branchName: "Main",
        experience: "operations",
      },
      routingPlan: null,
      workspaces: [
        {
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          displayName: "Mica Org",
          branches: [{ branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", name: "Main" }],
        },
      ],
      bindDestination,
    };

    renderWithBranchGate();

    expect(screen.getByTestId("catalog-ok")).toBeInTheDocument();
    expect(screen.queryByTestId("branch-required-panel")).not.toBeInTheDocument();
  });

  it("keeps children visible during soft workspace reload when already branch-bound", () => {
    workspaceMock = {
      status: "loading",
      boundWorkspace: {
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationDisplayName: "Mica Org",
        branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
        branchName: "Main",
        experience: "operations",
      },
      routingPlan: null,
      workspaces: [
        {
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          displayName: "Mica Org",
          branches: [{ branchId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", name: "Main" }],
        },
      ],
      bindDestination,
    };

    renderWithBranchGate();

    expect(screen.getByTestId("catalog-ok")).toBeInTheDocument();
    expect(screen.queryByText(/Checking session/i)).not.toBeInTheDocument();
  });

  it("redirects to branch setup for org-only bind with no branches even while status reports loading", async () => {
    workspaceMock = {
      status: "loading",
      boundWorkspace: {
        organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
        organizationDisplayName: "Mica Org",
        branchId: null,
        branchName: null,
        experience: "manage_business",
      },
      routingPlan: null,
      workspaces: [
        {
          organizationId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
          displayName: "Mica Org",
          branches: [],
        },
      ],
      bindDestination,
    };

    renderWithBranchGate("/inventory");

    expect(await screen.findByTestId("branches-page")).toBeInTheDocument();
    expect(screen.queryByTestId("inventory-ok")).not.toBeInTheDocument();
  });
});
