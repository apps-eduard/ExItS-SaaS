import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PlatformApiError } from "@/api/platform/platform-http";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import type { OrganizationAuditQuery } from "@/api/platform/organization-audit-query";

const ORG_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const OTHER_ORG = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";
const BRANCH_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

const getOrganizationAudit = vi.fn();
const listOrganizationBranches = vi.fn();

let membershipRole = "OrganizationOwner";
let organizationManagementAuthority = true;
let organizationId: string | null = ORG_ID;

vi.mock("@/api/platform/organization-audit-client", () => ({
  getOrganizationAudit: (...args: unknown[]) => getOrganizationAudit(...args),
}));

vi.mock("@/api/platform/organization-compliance-client", () => ({
  listOrganizationBranches: (...args: unknown[]) => listOrganizationBranches(...args),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: organizationId
      ? { organizationId, organizationDisplayName: "Mica Store", experience: "manage_business" }
      : null,
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: membershipRole === "OrganizationOwner" ? "Owner" : "Cashier",
      productLocalRoleCode: "Cashier",
      membershipRole,
      organizationManagementAuthority,
      featureCodes: [],
      grantedFeatureCodes: [],
    },
  }),
}));

const { OrgGovernanceAuditPage } = await import(
  "@/features/organization/audit/OrgGovernanceAuditPage"
);

function page(items = [row()], totalCount = items.length) {
  return { items, totalCount, page: 1, pageSize: 20 };
}

function row(id = "audit-1") {
  return {
    id,
    occurredAtUtc: "2026-10-01T08:00:00.000Z",
    actorIdentifier: "olivia.mendoza@exits.local",
    actionCode: "platform.organization.updated",
    targetType: "Organization",
    targetId: ORG_ID,
    organizationId: ORG_ID,
    outcome: "Succeeded",
    summary: "Profile name changed",
    reason: null,
  };
}

function renderPage(initialPath = "/org/audit") {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={[initialPath]}>
            <OrgGovernanceAuditPage />
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

function lastQuery(): OrganizationAuditQuery {
  const call = getOrganizationAudit.mock.calls.at(-1);
  return call?.[1] as OrganizationAuditQuery;
}

describe("OrgGovernanceAuditPage", () => {
  beforeEach(() => {
    membershipRole = "OrganizationOwner";
    organizationManagementAuthority = true;
    organizationId = ORG_ID;
    vi.clearAllMocks();
    listOrganizationBranches.mockResolvedValue([
      { id: BRANCH_ID, name: "Main", code: "MAIN", status: "Active" },
    ]);
    getOrganizationAudit.mockResolvedValue(page());
  });

  it("renders audit rows for the bound organization only", async () => {
    renderPage(`/org/audit?organizationId=${OTHER_ORG}`);
    const rows = await screen.findAllByTestId("audit-row");
    expect(rows.length).toBeGreaterThan(0);
    for (const row of rows) {
      expect(row).toHaveTextContent("Profile name changed");
      expect(row).toHaveTextContent("Organization profile updated");
    }
    await waitFor(() => expect(getOrganizationAudit).toHaveBeenCalled());
    expect(getOrganizationAudit.mock.calls[0]?.[0]).toBe(ORG_ID);
    expect(getOrganizationAudit.mock.calls[0]?.[0]).not.toBe(OTHER_ORG);
  });

  it("applies from, to, actor, action, outcome, and branch filters", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findAllByTestId("audit-row");
    await user.type(screen.getByLabelText("From"), "2026-10-01");
    await user.type(screen.getByLabelText("To"), "2026-10-02");
    await user.type(screen.getByLabelText("Actor"), "olivia");
    await user.type(screen.getByLabelText("Action"), "branch.updated");
    await user.selectOptions(screen.getByLabelText("Outcome"), "Denied");
    await user.selectOptions(screen.getByLabelText("Branch"), BRANCH_ID);
    await user.click(screen.getByTestId("audit-apply"));
    await waitFor(() => expect(lastQuery().actor).toBe("olivia"));
    expect(lastQuery()).toMatchObject({
      fromDate: "2026-10-01",
      toDate: "2026-10-02",
      action: "branch.updated",
      outcome: "Denied",
      branchId: BRANCH_ID,
      page: 1,
    });
    expect(getOrganizationAudit.mock.calls.at(-1)?.[0]).toBe(ORG_ID);
  });

  it("clears filters and pages forward", async () => {
    const user = userEvent.setup();
    getOrganizationAudit.mockResolvedValue(page([row()], 40));
    renderPage();
    await screen.findAllByTestId("audit-row");
    await user.type(screen.getByLabelText("Actor"), "olivia");
    await user.click(screen.getByTestId("audit-apply"));
    await waitFor(() => expect(lastQuery().actor).toBe("olivia"));
    await user.click(screen.getByTestId("audit-clear"));
    await waitFor(() => expect(lastQuery().actor).toBe(""));
    await user.click(screen.getByTestId("audit-next"));
    await waitFor(() => expect(lastQuery().page).toBe(2));
  });

  it("denies a cashier before calling the audit API", async () => {
    membershipRole = "OrganizationMember";
    organizationManagementAuthority = false;
    renderPage();
    expect(await screen.findByTestId("org-audit-denied")).toBeInTheDocument();
    expect(getOrganizationAudit).not.toHaveBeenCalled();
  });

  it("shows a denied state when the server returns 403", async () => {
    getOrganizationAudit.mockRejectedValue(new PlatformApiError(403, { detail: "Forbidden" }));
    renderPage();
    expect(await screen.findByTestId("org-audit-denied")).toBeInTheDocument();
  });

  it("shows an empty state", async () => {
    getOrganizationAudit.mockResolvedValue(page([]));
    renderPage();
    expect(await screen.findByTestId("audit-empty")).toBeInTheDocument();
  });

  it("shows a loading state until the audit page arrives", () => {
    getOrganizationAudit.mockReturnValue(new Promise(() => undefined));
    renderPage();
    expect(screen.getByTestId("audit-loading")).toBeInTheDocument();
  });

  it("retries after a load error", async () => {
    const user = userEvent.setup();
    getOrganizationAudit
      .mockRejectedValueOnce(new PlatformApiError(500, { detail: "unavailable" }))
      .mockResolvedValue(page());
    renderPage();
    expect(await screen.findByTestId("audit-load-error")).toBeInTheDocument();
    await user.click(screen.getByTestId("audit-retry"));
    expect((await screen.findAllByTestId("audit-row")).length).toBeGreaterThan(0);
  });
});
