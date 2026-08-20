import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { mockAuthenticatedFetch } from "@/test/auth-fixtures";

const sampleOrg = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
};

const auditSucceeded = {
  id: "11111111-1111-1111-1111-111111111111",
  occurredAtUtc: "2026-08-01T08:00:00Z",
  actorIdentifier: "olivia@example.test",
  actorType: "PlatformUser",
  actionCode: "platform.auth.login_succeeded",
  targetType: "PlatformAuthSession",
  targetId: "22222222-2222-2222-2222-222222222222",
  outcome: "Succeeded",
  summary: "Signed in successfully",
};

const auditDenied = {
  id: "33333333-3333-3333-3333-333333333333",
  occurredAtUtc: "2026-08-02T09:00:00Z",
  actorIdentifier: "platform-user:44444444-4444-4444-4444-444444444444",
  actorType: "PlatformUser",
  actionCode: "org.branch.update.very.long.action.code.that.must.wrap.safely",
  targetType: "OrganizationBranch",
  targetId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  outcome: "Denied",
  reason: "Not permitted",
};

const auditUnknown = {
  id: "55555555-5555-5555-5555-555555555555",
  occurredAtUtc: "2026-08-03T10:00:00Z",
  actorIdentifier: "cashier@ORG000001",
  actorType: "OrganizationMember",
  actionCode: "custom.unknown.event",
  targetType: "UnknownTargetType",
  targetId: "zzzz",
  outcome: "WeirdOutcome",
  summary: "Unknown mapped values stay visible",
};

function stubDesktop(desktop = true) {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches:
        (desktop && query.includes("min-width: 1024px")) ||
        (desktop && query.includes("min-width: 768px")),
      media: query,
      onchange: null,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      addListener: () => undefined,
      removeListener: () => undefined,
      dispatchEvent: () => true,
    } as MediaQueryList;
  });
}

describe("organization workspace activity audit", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("maps org audit records without mutation or export controls", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgAuditItems: [auditSucceeded, auditDenied, auditUnknown],
      orgAuditTotalCount: 3,
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}`);
    const user = userEvent.setup();
    render(<App />);
    const workspaceNav = await screen.findByRole("navigation", { name: "Organization workspace" });
    await user.click(within(workspaceNav).getByRole("link", { name: "Activity / Audit" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe(`/admin/organizations/${sampleOrg.id}/activity`);
    });
    expect(
      await screen.findByRole("heading", { name: "Activity / Audit", level: 1 }),
    ).toBeInTheDocument();
    expect(screen.getByText("Signed in")).toBeInTheDocument();
    expect(screen.getAllByText("Succeeded").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Denied").length).toBeGreaterThan(0);
    expect(screen.getByText("WeirdOutcome")).toBeInTheDocument();
    expect(screen.getByText("Custom unknown event")).toBeInTheDocument();
    expect(screen.getByText("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /export/i })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /delete/i })).not.toBeInTheDocument();
  });

  it("maps filters and pagination to the org audit URL and request", async () => {
    stubDesktop();
    const fetchMock = mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgAuditItems: [auditSucceeded],
      orgAuditTotalCount: 21,
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/activity`);
    const user = userEvent.setup();
    render(<App />);
    await screen.findByRole("heading", { name: "Activity / Audit", level: 1 });
    await user.type(screen.getByLabelText("Actor"), "olivia");
    await user.selectOptions(screen.getByLabelText("Outcome"), "Denied");
    await user.click(screen.getByRole("button", { name: "Apply filters" }));
    await waitFor(() => {
      expect(window.location.search).toContain("actor=olivia");
      expect(window.location.search).toContain("outcome=Denied");
    });
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      const urls = fetchMock.mock.calls.map(([input]) => String(input));
      expect(urls.some((url) => url.includes("/audit?") && url.includes("actor=olivia"))).toBe(
        true,
      );
      expect(urls.some((url) => url.includes("outcome=Denied"))).toBe(true);
      expect(urls.some((url) => url.includes("page=2") && url.includes("pageSize=20"))).toBe(true);
    });
  });

  it("shows empty and zero-result states", async () => {
    stubDesktop();
    mockAuthenticatedFetch({ organizationItems: [sampleOrg], orgAuditItems: [] });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/activity`);
    const { unmount } = render(<App />);
    expect(await screen.findByText("No audit records")).toBeInTheDocument();
    unmount();

    mockAuthenticatedFetch({ organizationItems: [sampleOrg], orgAuditItems: [] });
    window.history.replaceState(
      {},
      "",
      `/admin/organizations/${sampleOrg.id}/activity?actor=nobody`,
    );
    render(<App />);
    expect(await screen.findByText("No audit records match your filters.")).toBeInTheDocument();
  });

  it("fail-closes forbidden audit without leaking payload", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      forbiddenOrgAudit: true,
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/activity`);
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Page not found" })).toBeInTheDocument();
    expect(screen.queryByText("audit-secret")).not.toBeInTheDocument();
  });

  it("renders mobile cards and Filipino labels", async () => {
    stubDesktop(false);
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      orgAuditItems: [auditSucceeded],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/activity`);
    const user = userEvent.setup();
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Activity / Audit", level: 1 }),
    ).toBeInTheDocument();
    expect(await screen.findByText("Signed in successfully")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Preferences" }));
    await user.click(await screen.findByRole("menuitem", { name: /Filipino/i }));
    expect(
      await screen.findByRole("heading", { name: "Aktibidad / Audit", level: 1 }),
    ).toBeInTheDocument();
  });
});
