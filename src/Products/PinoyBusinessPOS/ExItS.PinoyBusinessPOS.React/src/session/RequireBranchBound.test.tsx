import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
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

let workspaceMock: {
  status: string;
  boundWorkspace: {
    organizationId: string;
    organizationDisplayName: string;
    branchId: string | null;
    branchName: string | null;
    experience: "manage_business" | "operations";
  } | null;
  routingPlan: null;
};

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => workspaceMock,
}));

describe("RequireBranchBound", () => {
  it("shows branch-required panel for Manage Business (org-only) bind", () => {
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
    expect(screen.queryByTestId("catalog-ok")).not.toBeInTheDocument();
    expect(screen.queryByText(/Checking session/i)).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-required-choose-workspace")).toBeInTheDocument();
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

    expect(screen.getByTestId("catalog-ok")).toBeInTheDocument();
    expect(screen.queryByTestId("branch-required-panel")).not.toBeInTheDocument();
  });
});
