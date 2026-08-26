import { render, screen } from "@testing-library/react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import { describe, expect, it, vi } from "vitest";
import { RequireProcessReturn, RequireViewReturns } from "@/session/SessionGuards";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";

const workspaceState = vi.hoisted(() => ({
  sessionGrant: null as SessionGrantResponse | null,
}));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    status: "authenticated",
    session: { accountClass: "Organization", email: "owner@example.com" },
    signIn: vi.fn(),
    signOut: vi.fn(),
    refreshSession: vi.fn(),
  }),
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
    locale: "en",
    setLocale: vi.fn(),
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    status: "ready",
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      branchId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    },
    routingPlan: null,
    sessionGrant: workspaceState.sessionGrant,
  }),
}));

function grant(partial: Partial<SessionGrantResponse>): SessionGrantResponse {
  return {
    accessToken: "token",
    productAccessAllowed: true,
    ...partial,
  };
}

describe("SessionGuards returns", () => {
  it("allows Cashier ViewReturns and denies ProcessReturn", () => {
    workspaceState.sessionGrant = grant({
      mappedPosRoleCode: "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole: "OrganizationMember",
    });

    const { unmount } = render(
      <MemoryRouter initialEntries={["/view"]}>
        <Routes>
          <Route
            path="/view"
            element={
              <RequireViewReturns>
                <div data-testid="view-ok">view-ok</div>
              </RequireViewReturns>
            }
          />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByTestId("view-ok")).toBeInTheDocument();
    unmount();

    render(
      <MemoryRouter initialEntries={["/process"]}>
        <Routes>
          <Route
            path="/process"
            element={
              <RequireProcessReturn>
                <div data-testid="process-ok">process-ok</div>
              </RequireProcessReturn>
            }
          />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByTestId("returns-process-denied")).toBeInTheDocument();
    expect(screen.queryByTestId("process-ok")).not.toBeInTheDocument();
  });

  it("allows Owner ProcessReturn", () => {
    workspaceState.sessionGrant = grant({
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      membershipRole: "OrganizationOwner",
      organizationManagementAuthority: true,
    });
    render(
      <MemoryRouter initialEntries={["/process"]}>
        <Routes>
          <Route
            path="/process"
            element={
              <RequireProcessReturn>
                <div data-testid="process-ok">process-ok</div>
              </RequireProcessReturn>
            }
          />
        </Routes>
      </MemoryRouter>,
    );
    expect(screen.getByTestId("process-ok")).toBeInTheDocument();
  });
});
