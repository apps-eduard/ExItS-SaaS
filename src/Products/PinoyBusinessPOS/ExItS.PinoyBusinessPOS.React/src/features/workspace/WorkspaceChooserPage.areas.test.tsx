import "fake-indexeddb/auto";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { SessionProvider } from "@/session/SessionProvider";
import { WorkspaceProvider } from "@/workspace/WorkspaceProvider";
import { WorkspaceChooserPage } from "@/features/workspace/WorkspaceChooserPage";
import { jsonResponse } from "@/test/render";
import { isOfflinePinAndDekConfigured } from "@/offline/local-store-key";
import * as branchClient from "@/api/platform/organization-branches-client";

const ORG_ID = "11111111-1111-1111-1111-111111111111";
const PANAY_ID = "aaaa1111-1111-1111-1111-111111111111";
const VISAYAS_ID = "aaaa2222-2222-2222-2222-222222222222";
const MAIN_ID = "bbbb1111-1111-1111-1111-111111111111";
const ILOILO_ID = "bbbb2222-2222-2222-2222-222222222222";
const CEBU_ID = "bbbb3333-3333-3333-3333-333333333333";
const MANILA_ID = "bbbb4444-4444-4444-4444-444444444444";

const probeMock = vi.fn();
const listEligibleOrganizations = vi.fn();
const listOrganizationBranches = vi.fn();

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
    listEligibleOrganizations: (...args: unknown[]) => listEligibleOrganizations(...args),
    listOrganizationBranches: (...args: unknown[]) => listOrganizationBranches(...args),
    probeOrganizationSessionGrant: (...args: unknown[]) => probeMock(...args),
  };
});

vi.mock("@/api/platform/organization-branches-client", async (importOriginal) => {
  const actual =
    await importOriginal<typeof import("@/api/platform/organization-branches-client")>();
  return {
    ...actual,
    listBranchManagementSummaries: vi.fn(),
  };
});

function branch(
  id: string,
  code: string,
  name: string,
  areaId: string | null,
  areaName: string | null,
) {
  return {
    id,
    organizationId: ORG_ID,
    code,
    name,
    isPrimary: id === MAIN_ID,
    status: "Active",
    areaId,
    areaName,
  };
}

function areaBranches() {
  return [
    branch(MAIN_ID, "MAIN", "Main", PANAY_ID, "PANAY"),
    branch(ILOILO_ID, "ILO", "Iloilo", PANAY_ID, "PANAY"),
    branch(CEBU_ID, "CEB", "Cebu", VISAYAS_ID, "VISAYAS"),
    branch(MANILA_ID, "MNL", "Manila", null, null),
  ];
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

describe("workspace chooser area grouping", () => {
  beforeEach(() => {
    probeMock.mockReset();
    listEligibleOrganizations.mockReset();
    listOrganizationBranches.mockReset();
    vi.mocked(branchClient.listBranchManagementSummaries).mockReset();
    vi.mocked(isOfflinePinAndDekConfigured).mockReturnValue(true);

    listEligibleOrganizations.mockResolvedValue({
      ok: true,
      organizations: [
        {
          organizationId: ORG_ID,
          displayName: "Kizy Store",
          slug: "kizy-store",
          membershipRole: "OrganizationMember",
        },
      ],
    });
    listOrganizationBranches.mockResolvedValue({ ok: true, branches: areaBranches() });
    vi.mocked(branchClient.listBranchManagementSummaries).mockResolvedValue({
      ok: false,
      status: 403,
      body: { detail: "forbidden" },
    });
    probeMock.mockResolvedValue({
      ok: true as const,
      grant: {
        accessToken: "probe-token",
        productAccessAllowed: true,
        mappedPosRoleCode: "StoreManager",
        productLocalRoleCode: "StoreManager",
        organizationManagementAuthority: false,
        membershipRole: "OrganizationMember",
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
            username: "manager",
            displayName: "Manager One",
            email: "manager@example.com",
            accountClass: "Organization",
            homeOrganizationId: ORG_ID,
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

  it("AREA02 chooser groups authorized branches by area", async () => {
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId("workspace-area-groups")).toBeInTheDocument();
    });

    const panay = screen.getByTestId(`workspace-area-group-${PANAY_ID}`);
    expect(within(panay).getByRole("heading", { name: "PANAY" })).toBeInTheDocument();
    expect(within(panay).getByTestId(`workspace-branch-${MAIN_ID}`)).toBeInTheDocument();
    expect(within(panay).getByTestId(`workspace-branch-${ILOILO_ID}`)).toBeInTheDocument();
    expect(within(panay).queryByTestId(`workspace-branch-${CEBU_ID}`)).not.toBeInTheDocument();

    const visayas = screen.getByTestId(`workspace-area-group-${VISAYAS_ID}`);
    expect(within(visayas).getByTestId(`workspace-branch-${CEBU_ID}`)).toBeInTheDocument();
  });

  it("AREA02 chooser puts unassigned last and keeps areas unselectable", async () => {
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId("workspace-area-group-unassigned")).toBeInTheDocument();
    });

    const groups = screen.getByTestId("workspace-area-groups");
    const rendered = within(groups)
      .getAllByRole("heading", { level: 4 })
      .map((heading) => heading.textContent);
    expect(rendered).toEqual(["PANAY", "VISAYAS", "Unassigned"]);

    const unassigned = screen.getByTestId("workspace-area-group-unassigned");
    expect(within(unassigned).getByTestId(`workspace-branch-${MANILA_ID}`)).toBeInTheDocument();
    // The heading carries no destination: only a branch card offers Operations / Start selling.
    expect(screen.queryByTestId(`workspace-branch-${PANAY_ID}`)).not.toBeInTheDocument();
    expect(within(groups).getAllByTestId("workspace-destination-start_selling")).toHaveLength(4);
  });

  it("AREA02 chooser hides unassigned when no area-less branch is authorized", async () => {
    listOrganizationBranches.mockResolvedValue({
      ok: true,
      branches: areaBranches().filter((b) => b.id !== MANILA_ID),
    });
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId(`workspace-area-group-${PANAY_ID}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId("workspace-area-group-unassigned")).not.toBeInTheDocument();
    expect(screen.queryByText("Manila")).not.toBeInTheDocument();
  });

  it("AREA02 chooser keeps a flat list when the organization has no areas", async () => {
    listOrganizationBranches.mockResolvedValue({
      ok: true,
      branches: [
        branch(MAIN_ID, "MAIN", "Main", null, null),
        branch(ILOILO_ID, "ILO", "Iloilo", null, null),
      ],
    });
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId(`workspace-branch-${MAIN_ID}`)).toBeInTheDocument();
    });
    expect(screen.queryByTestId("workspace-area-groups")).not.toBeInTheDocument();
    expect(screen.getByTestId(`workspace-branch-${ILOILO_ID}`)).toBeInTheDocument();
  });

  it("AREA02 chooser reads the branch list once for grouping", async () => {
    renderWorkspaceChooser();

    await waitFor(() => {
      expect(screen.getByTestId("workspace-area-groups")).toBeInTheDocument();
    });
    expect(listOrganizationBranches).toHaveBeenCalledTimes(1);
  });
});
