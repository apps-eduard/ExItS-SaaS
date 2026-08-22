import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { SessionProvider } from "@/session/SessionProvider";
import { WorkspaceProvider } from "@/workspace/WorkspaceProvider";
import { WorkspaceChooserPage } from "@/features/workspace/WorkspaceChooserPage";
import { jsonResponse } from "@/test/render";

const E2E_ORG_ID = "11111111-1111-1111-1111-111111111111";
const E2E_BRANCH_ID = "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa";
const E2E_BRANCH_2_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";

const probeMock = vi.fn();

vi.mock("@/offline/local-store-key", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/offline/local-store-key")>();
  return {
    ...actual,
    isOfflinePinAndDekConfigured: vi.fn(() => true),
  };
});

vi.mock("@/api/platform/platform-auth-client", async (importOriginal) => {
  const actual = await importOriginal<typeof import("@/api/platform/platform-auth-client")>();
  return {
    ...actual,
    listEligibleOrganizations: vi.fn(async () => ({
      ok: true as const,
      organizations: [
        {
          organizationId: E2E_ORG_ID,
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
          id: E2E_BRANCH_ID,
          organizationId: E2E_ORG_ID,
          code: "MAIN",
          name: "Main Branch",
          isPrimary: true,
          status: "Active",
        },
        {
          id: E2E_BRANCH_2_ID,
          organizationId: E2E_ORG_ID,
          code: "K02",
          name: "Kizy Store 02",
          isPrimary: false,
          status: "Active",
        },
      ],
    })),
    probeOrganizationSessionGrant: (...args: unknown[]) => probeMock(...args),
    listOrganizationMembers: vi.fn(async () => ({ ok: true as const, members: [] })),
  };
});

function ownerGrantProbe() {
  return {
    ok: true as const,
    grant: {
      accessToken: "probe-token",
      productAccessAllowed: true,
      mappedPosRoleCode: "Owner",
      productLocalRoleCode: "Owner",
      organizationManagementAuthority: true,
      membershipRole: "OrganizationOwner",
    },
  };
}

function renderWorkspaceChooser() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <SessionProvider>
            <MemoryRouter initialEntries={["/workspace"]}>
              <WorkspaceProvider>
                <WorkspaceChooserPage />
              </WorkspaceProvider>
            </MemoryRouter>
          </SessionProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("workspace grant hint regression", () => {
  beforeEach(() => {
    probeMock.mockReset();
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
            homeOrganizationId: E2E_ORG_ID,
            organizationContextLocked: false,
          });
        }
        if (url.includes("/api/v1/platform/antiforgery/token")) {
          return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "csrf-token" });
        }
        return jsonResponse(404, { detail: "not mocked" });
      }),
    );
  });

  it("shows grant probe error with retry instead of silent branch-only cards", async () => {
    probeMock.mockResolvedValue({ ok: false, status: 503, body: { detail: "network down" } });
    renderWorkspaceChooser();
    await waitFor(() => {
      expect(screen.getByTestId("workspace-grant-probe-error")).toBeInTheDocument();
    });
    expect(screen.queryByTestId("workspace-destination-manage_business")).not.toBeInTheDocument();
    expect(screen.queryByText("Main Branch")).not.toBeInTheDocument();
    expect(screen.getByText("2 branches")).toBeInTheDocument();
  });

  it("retry reloads owner destinations after initial probe failure", async () => {
    probeMock
      .mockResolvedValueOnce({ ok: false, status: 503, body: { detail: "network down" } })
      .mockResolvedValueOnce(ownerGrantProbe());
    const user = userEvent.setup();
    renderWorkspaceChooser();
    await waitFor(() => {
      expect(screen.getByTestId("workspace-grant-probe-error")).toBeInTheDocument();
    });
    await user.click(screen.getByRole("button", { name: "Retry authorization" }));
    await waitFor(() => {
      expect(screen.getByTestId("workspace-destination-manage_business")).toBeInTheDocument();
    });
    expect(screen.getAllByTestId("workspace-destination-operations")).toHaveLength(2);
    expect(screen.getAllByTestId("workspace-destination-start_selling")).toHaveLength(2);
  });

  it("loads owner destinations when grant probe succeeds", async () => {
    probeMock.mockResolvedValue(ownerGrantProbe());
    renderWorkspaceChooser();
    await waitFor(() => {
      expect(screen.getByTestId("workspace-destination-manage_business")).toBeInTheDocument();
    });
    expect(screen.getByText("Kizy Store")).toBeInTheDocument();
    expect(screen.getByText("Owner")).toBeInTheDocument();
    expect(screen.getAllByTestId("workspace-destination-operations")).toHaveLength(2);
  });
});
