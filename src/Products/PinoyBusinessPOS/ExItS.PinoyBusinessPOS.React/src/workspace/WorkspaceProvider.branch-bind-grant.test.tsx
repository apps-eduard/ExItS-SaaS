import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { act, render, screen, waitFor } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import {
  canCreateSale,
  canEnterManagerRoleHome,
  resolveEffectivePosRoleCode,
} from "@/access/pos-capabilities";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { RequireManagerRoleHome } from "@/session/SessionGuards";
import { SessionProvider } from "@/session/SessionProvider";
import type { SessionGrantResponse } from "@/api/platform/platform-auth-client";
import { clearPosSessionGrant, getPosSessionGrant, setPosSessionGrant } from "@/api/platform/pos-session-grant";
import { jsonResponse } from "@/test/session-context";
import { useWorkspace, WorkspaceProvider } from "@/workspace/WorkspaceProvider";

const ORG_ID = "11111111-1111-1111-1111-111111111111";
const BRANCH_A = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const BRANCH_B = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
const WAREHOUSE_BRANCH = "cccccccc-cccc-cccc-cccc-cccccccccccc";

const bindWorkspaceWithSessionGrant = vi.fn();
const selectOperationalBranch = vi.fn();
const hydratePosDeviceContext = vi.fn();

vi.mock("@/api/platform/platform-auth-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/platform/platform-auth-client")>();
  return {
    ...actual,
    listEligibleOrganizations: vi.fn(async () => ({
      ok: true as const,
      organizations: [
        {
          organizationId: ORG_ID,
          displayName: "Kizy Store",
          slug: "kizy-store",
          membershipRole: "OrganizationOwner",
        },
      ],
    })),
    listOrganizationBranches: vi.fn(async () => ({
      ok: true as const,
      branches: [
        {
          id: BRANCH_A,
          organizationId: ORG_ID,
          code: "A",
          name: "Branch A",
          isPrimary: true,
          status: "Active",
          branchType: "Retail",
        },
        {
          id: BRANCH_B,
          organizationId: ORG_ID,
          code: "B",
          name: "Branch B",
          isPrimary: false,
          status: "Active",
          branchType: "Retail",
        },
        {
          id: WAREHOUSE_BRANCH,
          organizationId: ORG_ID,
          code: "WH",
          name: "Warehouse",
          isPrimary: false,
          status: "Active",
          branchType: "Warehouse",
        },
      ],
    })),
    probeOrganizationSessionGrant: vi.fn(async () => ({
      ok: true as const,
      grant: ownerGrant("probe-token"),
    })),
    bindWorkspaceWithSessionGrant: (...args: unknown[]) => bindWorkspaceWithSessionGrant(...args),
    bindOrganizationManagementGrant: vi.fn(),
  };
});

vi.mock("@/api/pos/operational-branch-client", () => ({
  selectOperationalBranch: (...args: unknown[]) => selectOperationalBranch(...args),
}));

vi.mock("@/workspace/hydrate-pos-device", () => ({
  hydratePosDeviceContext: (...args: unknown[]) => hydratePosDeviceContext(...args),
}));

vi.mock("@/offline/local-store-key", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/offline/local-store-key")>();
  return {
    ...actual,
    isOfflinePinAndDekConfigured: vi.fn(() => false),
  };
});

function ownerGrant(token: string, branchId = BRANCH_B) {
  return {
    accessToken: token,
    productAccessAllowed: true,
    mappedPosRoleCode: "Owner",
    productLocalRoleCode: "Owner",
    organizationManagementAuthority: true,
    membershipRole: "OrganizationOwner",
    organizationId: ORG_ID,
    branchId,
  };
}

function persistOkGrant(grant: SessionGrantResponse) {
  // Mirror production bindWorkspaceWithSessionGrant persistence.
  setPosSessionGrant(grant);
  return { ok: true as const, grant };
}

function managerGrant(token: string) {
  return {
    accessToken: token,
    productAccessAllowed: true,
    mappedPosRoleCode: "StoreManager",
    productLocalRoleCode: "Manager",
    organizationManagementAuthority: false,
    membershipRole: "OrganizationMember",
    organizationId: ORG_ID,
    branchId: BRANCH_B,
  };
}

function cashierGrant(token: string) {
  return {
    accessToken: token,
    productAccessAllowed: true,
    mappedPosRoleCode: "Cashier",
    productLocalRoleCode: "Cashier",
    organizationManagementAuthority: false,
    membershipRole: "OrganizationMember",
    organizationId: ORG_ID,
    branchId: BRANCH_B,
  };
}

function BindProbe() {
  const { status, sessionGrant, boundWorkspace, bindDestination } = useWorkspace();
  return (
    <div>
      <div data-testid="ws-status">{status}</div>
      <div data-testid="ws-branch">{boundWorkspace?.branchId ?? "none"}</div>
      <div data-testid="ws-branch-type">{boundWorkspace?.branchType ?? "none"}</div>
      <div data-testid="ws-role">{resolveEffectivePosRoleCode(sessionGrant) ?? "none"}</div>
      <div data-testid="ws-ops">{canEnterManagerRoleHome(sessionGrant) ? "yes" : "no"}</div>
      <div data-testid="ws-sale">
        {canCreateSale(sessionGrant, boundWorkspace?.branchType) ? "yes" : "no"}
      </div>
      <div data-testid="ws-token">{sessionGrant?.accessToken ?? "none"}</div>
      <button
        type="button"
        data-testid="switch-retail-b"
        onClick={() =>
          void bindDestination({
            organizationId: ORG_ID,
            organizationDisplayName: "Kizy Store",
            branchId: BRANCH_B,
            branchName: "Branch B",
            experience: "operations",
            route: "/role/manager",
            labelKey: "experience.operations",
          })
        }
      >
        Switch B
      </button>
      <button
        type="button"
        data-testid="switch-warehouse"
        onClick={() =>
          void bindDestination({
            organizationId: ORG_ID,
            organizationDisplayName: "Kizy Store",
            branchId: WAREHOUSE_BRANCH,
            branchName: "Warehouse",
            experience: "operations",
            route: "/role/manager",
            labelKey: "experience.operations",
          })
        }
      >
        Switch WH
      </button>
    </div>
  );
}

function renderHarness(initialPath = "/probe") {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <SessionProvider>
            <MemoryRouter initialEntries={[initialPath]}>
              <WorkspaceProvider>
                <Routes>
                  <Route path="/probe" element={<BindProbe />} />
                  <Route
                    path="/role/manager"
                    element={
                      <RequireManagerRoleHome>
                        <div data-testid="manager-home-ok">Operations OK</div>
                      </RequireManagerRoleHome>
                    }
                  />
                </Routes>
              </WorkspaceProvider>
            </MemoryRouter>
          </SessionProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("WorkspaceProvider branch bind keeps sessionGrant current", () => {
  beforeEach(() => {
    clearPosSessionGrant();
    bindWorkspaceWithSessionGrant.mockReset();
    selectOperationalBranch.mockReset();
    hydratePosDeviceContext.mockReset();
    hydratePosDeviceContext.mockResolvedValue({
      status: "unknown",
      registrationStatus: "unknown",
      installationDeviceId: null,
      posDeviceId: null,
      detail: null,
    });
    selectOperationalBranch.mockResolvedValue({
      ok: true,
      context: {
        organizationId: ORG_ID,
        branchId: BRANCH_B,
        branchType: "Retail",
      },
    });
    vi.stubGlobal(
      "fetch",
      vi.fn(async (input: RequestInfo | URL) => {
        const url = String(input);
        if (url.includes("/api/v1/platform/auth/me")) {
          return jsonResponse(200, {
            sessionId: "22222222-2222-2222-2222-222222222222",
            userId: "cccccccc-cccc-4ccc-8ccc-cccccccccccc",
            username: "owner",
            displayName: "Owner One",
            email: "owner@example.com",
            accountClass: "Organization",
            homeOrganizationId: ORG_ID,
            organizationContextLocked: false,
          });
        }
        if (url.includes("/api/v1/platform/antiforgery/token")) {
          return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        if (url.includes("/api/v1/pos/runtime-policy/device-authorization")) {
          return jsonResponse(200, { enforcementEnabled: false });
        }
        return jsonResponse(404, { detail: "not mocked" });
      }),
    );
  });

  it("Owner branch A→B updates live sessionGrant and keeps Owner role with Operations access", async () => {
    const fresh = ownerGrant("fresh-branch-b-token", BRANCH_B);
    bindWorkspaceWithSessionGrant.mockResolvedValue(persistOkGrant(fresh));

    renderHarness();
    await waitFor(() => {
      expect(screen.getByTestId("ws-status").textContent).toMatch(/ready|bound/);
    });

    await act(async () => {
      screen.getByTestId("switch-retail-b").click();
    });

    await waitFor(() => {
      expect(screen.getByTestId("ws-token")).toHaveTextContent("fresh-branch-b-token");
    });
    expect(screen.getByTestId("ws-status")).toHaveTextContent("bound");
    expect(screen.getByTestId("ws-branch")).toHaveTextContent(BRANCH_B);
    expect(screen.getByTestId("ws-role")).toHaveTextContent("Owner");
    expect(screen.getByTestId("ws-ops")).toHaveTextContent("yes");
    expect(getPosSessionGrant()?.accessToken).toBe("fresh-branch-b-token");
  });

  it("Owner can enter /role/manager after branch switch (fresh grant, not stale)", async () => {
    bindWorkspaceWithSessionGrant.mockResolvedValue(
      persistOkGrant(ownerGrant("ops-token", BRANCH_B)),
    );

    const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
    function SwitchThenNavigate() {
      const { bindDestination, sessionGrant, status } = useWorkspace();
      return (
        <div>
          <div data-testid="nav-ops">{canEnterManagerRoleHome(sessionGrant) ? "yes" : "no"}</div>
          <div data-testid="nav-status">{status}</div>
          <button
            type="button"
            data-testid="go-ops"
            onClick={() => {
              void bindDestination({
                organizationId: ORG_ID,
                organizationDisplayName: "Kizy Store",
                branchId: BRANCH_B,
                branchName: "Branch B",
                experience: "operations",
                route: "/role/manager",
                labelKey: "experience.operations",
              }).then((ok) => {
                if (ok) {
                  window.history.pushState({}, "", "/role/manager");
                  window.dispatchEvent(new PopStateEvent("popstate"));
                }
              });
            }}
          >
            Go
          </button>
        </div>
      );
    }

    render(
      <QueryClientProvider client={client}>
        <PreferencesProvider>
          <I18nProvider>
            <SessionProvider>
              <MemoryRouter initialEntries={["/probe"]}>
                <WorkspaceProvider>
                  <Routes>
                    <Route path="/probe" element={<SwitchThenNavigate />} />
                    <Route
                      path="/role/manager"
                      element={
                        <RequireManagerRoleHome>
                          <div data-testid="manager-home-ok">Operations OK</div>
                        </RequireManagerRoleHome>
                      }
                    />
                  </Routes>
                </WorkspaceProvider>
              </MemoryRouter>
            </SessionProvider>
          </I18nProvider>
        </PreferencesProvider>
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(screen.getByTestId("nav-status").textContent).toMatch(/ready|bound/);
    });

    await act(async () => {
      screen.getByTestId("go-ops").click();
    });

    await waitFor(() => {
      expect(screen.getByTestId("nav-ops")).toHaveTextContent("yes");
    });
  });

  it("Manager branch switch remains allowed for Operations", async () => {
    bindWorkspaceWithSessionGrant.mockResolvedValue(persistOkGrant(managerGrant("manager-token")));
    renderHarness();
    await waitFor(() => {
      expect(screen.getByTestId("ws-status").textContent).toMatch(/ready|bound/);
    });
    await act(async () => {
      screen.getByTestId("switch-retail-b").click();
    });
    await waitFor(() => {
      expect(screen.getByTestId("ws-token")).toHaveTextContent("manager-token");
    });
    expect(screen.getByTestId("ws-role")).toHaveTextContent(/StoreManager|Manager/);
    expect(screen.getByTestId("ws-ops")).toHaveTextContent("yes");
  });

  it("Cashier branch switch does not grant Operations access", async () => {
    bindWorkspaceWithSessionGrant.mockResolvedValue(persistOkGrant(cashierGrant("cashier-token")));
    renderHarness();
    await waitFor(() => {
      expect(screen.getByTestId("ws-status").textContent).toMatch(/ready|bound/);
    });
    await act(async () => {
      screen.getByTestId("switch-retail-b").click();
    });
    await waitFor(() => {
      expect(screen.getByTestId("ws-token")).toHaveTextContent("cashier-token");
    });
    expect(screen.getByTestId("ws-role")).toHaveTextContent("Cashier");
    expect(screen.getByTestId("ws-ops")).toHaveTextContent("no");
  });

  it("Owner Warehouse switch allows Operations but denies CreateSale", async () => {
    bindWorkspaceWithSessionGrant.mockResolvedValue(
      persistOkGrant(ownerGrant("wh-token", WAREHOUSE_BRANCH)),
    );
    selectOperationalBranch.mockResolvedValue({
      ok: true,
      context: {
        organizationId: ORG_ID,
        branchId: WAREHOUSE_BRANCH,
        branchType: "Warehouse",
      },
    });

    renderHarness();
    await waitFor(() => {
      expect(screen.getByTestId("ws-status").textContent).toMatch(/ready|bound/);
    });
    await act(async () => {
      screen.getByTestId("switch-warehouse").click();
    });
    await waitFor(() => {
      expect(screen.getByTestId("ws-token")).toHaveTextContent("wh-token");
    });
    expect(screen.getByTestId("ws-branch-type")).toHaveTextContent("Warehouse");
    expect(screen.getByTestId("ws-ops")).toHaveTextContent("yes");
    expect(screen.getByTestId("ws-sale")).toHaveTextContent("no");
    expect(screen.getByTestId("ws-role")).toHaveTextContent("Owner");
  });

  it("failed branch bind does not install the new grant into live React state", async () => {
    const stale = ownerGrant("stale-token", BRANCH_A);
    clearPosSessionGrant();
    // Seed a prior live grant via successful first bind, then fail the second.
    bindWorkspaceWithSessionGrant
      .mockResolvedValueOnce(persistOkGrant(stale))
      .mockResolvedValueOnce({
        ok: false,
        reason: "grant",
        status: 403,
        body: { errorCode: "application.auth.product_access_denied", detail: "denied" },
      });
    selectOperationalBranch.mockResolvedValue({
      ok: true,
      context: { organizationId: ORG_ID, branchId: BRANCH_A, branchType: "Retail" },
    });

    renderHarness();
    await waitFor(() => {
      expect(screen.getByTestId("ws-status").textContent).toMatch(/ready|bound/);
    });

    await act(async () => {
      screen.getByTestId("switch-retail-b").click();
    });
    await waitFor(() => {
      expect(screen.getByTestId("ws-token")).toHaveTextContent("stale-token");
    });

    await act(async () => {
      screen.getByTestId("switch-warehouse").click();
    });
    await waitFor(() => {
      expect(screen.getByTestId("ws-status").textContent).not.toBe("binding");
    });
    // Failed bind must not promote a new/stale grant into sessionGrant.
    expect(screen.getByTestId("ws-token")).toHaveTextContent("stale-token");
    expect(screen.queryByTestId("ws-token")).not.toHaveTextContent("fresh-fail-token");
  });
});
