import { afterEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  sampleAuthorization,
  sampleSession,
  textResponse,
} from "@/test/auth-fixtures";

const sampleOrg = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  displayName: "Northwind Market",
  slug: "northwind-market",
  status: "Active",
};

const member = {
  id: "11111111-1111-1111-1111-111111111111",
  organizationId: sampleOrg.id,
  userId: "22222222-2222-2222-2222-222222222222",
  role: "OrganizationMember",
  status: "Active",
  displayName: "Ana Cruz",
  email: "ana@org.test",
  roleDisplay: "Staff",
};

const invitation = {
  id: "33333333-3333-3333-3333-333333333333",
  organizationId: sampleOrg.id,
  email: "invitee@example.test",
  role: "OrganizationMember",
  status: "Pending",
  invitationStatus: "Sent",
  roleDisplay: "Staff",
  expiresAtUtc: "2026-09-01T00:00:00Z",
  acceptToken: "super-secret-accept-token",
};

function stubDesktop(table = true) {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px") || (table && query.includes("min-width: 768px")),
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

describe("organization workspace people", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("navigates Overview, Branches, and People and maps member fields", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      memberItems: [member],
      invitationItems: [invitation],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Northwind Market" })).toBeInTheDocument();
    const workspaceNav = screen.getByRole("navigation", { name: "Organization workspace" });
    await user.click(within(workspaceNav).getByRole("link", { name: "People" }));
    await waitFor(() => {
      expect(window.location.pathname).toBe(`/admin/organizations/${sampleOrg.id}/people`);
    });
    expect(await screen.findByRole("heading", { name: "People", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Ana Cruz")).toBeInTheDocument();
    expect(screen.getByText("Staff")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /invite/i })).toBeInTheDocument();
    expect(screen.queryByLabelText(/^Search$/i)).not.toBeInTheDocument();
    const crumb = screen.getByRole("navigation", { name: "Breadcrumb" });
    expect(crumb).toHaveTextContent("Organizations");
    expect(crumb).toHaveTextContent("Northwind Market");
    expect(crumb).toHaveTextContent("People");
    await user.click(within(workspaceNav).getByRole("link", { name: "Overview" }));
    expect(await screen.findByRole("heading", { name: "Northwind Market" })).toBeInTheDocument();
  });

  it("pages members with supported query params only", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/members")) {
        expect(url).toMatch(/page=\d+/);
        expect(url).toMatch(/pageSize=20/);
        expect(url).not.toMatch(/search=/);
        return jsonResponse(200, {
          items: [member],
          totalCount: 21,
          page: 1,
          pageSize: 20,
        });
      }
      if (url.includes("/invitations")) {
        return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 20 });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/people`);
    const user = userEvent.setup();
    render(<App />);
    expect(await screen.findByText("Ana Cruz")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Next" }));
    await waitFor(() => {
      expect(window.location.search).toContain("membersPage=2");
    });
    expect(
      fetchMock.mock.calls.some(
        ([input]) => String(input).includes("/members") && String(input).includes("page=2"),
      ),
    ).toBe(true);
  });

  it("filters invitations by status and never renders accept tokens", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("/commercial-summary")) {
        return jsonResponse(200, { subscriptions: [], payments: [], latestEntitlements: [] });
      }
      if (url.includes("/members")) {
        return jsonResponse(200, { items: [member], totalCount: 1, page: 1, pageSize: 20 });
      }
      if (url.includes("/invitations")) {
        return jsonResponse(200, {
          items: [invitation],
          totalCount: 1,
          page: 1,
          pageSize: 20,
        });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/people`);
    const user = userEvent.setup();
    render(<App />);
    await user.click(await screen.findByRole("tab", { name: "Invitations" }));
    expect(await screen.findByText("invitee@example.test")).toBeInTheDocument();
    expect(screen.queryByText("super-secret-accept-token")).not.toBeInTheDocument();
    await user.selectOptions(screen.getByLabelText("Invitation status"), "Pending");
    await waitFor(() => {
      expect(window.location.search).toContain("tab=invitations");
      expect(window.location.search).toContain("invitationsStatus=Pending");
    });
    expect(
      fetchMock.mock.calls.some(
        ([input]) =>
          String(input).includes("/invitations") && String(input).includes("status=Pending"),
      ),
    ).toBe(true);
  });

  it("keeps invitations visible when members fail", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      failMembers: true,
      invitationItems: [invitation],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/people`);
    const user = userEvent.setup();
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Unable to load members." }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Retry" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Copy error details" })).toBeInTheDocument();
    await user.click(screen.getByRole("tab", { name: "Invitations" }));
    expect(await screen.findByText("invitee@example.test")).toBeInTheDocument();
  });

  it("fail-closes forbidden member lists without leaking payload", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      forbiddenMembers: true,
      invitationItems: [invitation],
    });
    window.history.replaceState({}, "", `/admin/organizations/${sampleOrg.id}/people`);
    render(<App />);
    expect(await screen.findByText("This list is not available.")).toBeInTheDocument();
    expect(screen.queryByText("member-secret")).not.toBeInTheDocument();
  });

  it("does not call people APIs for an invalid organization id", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, sampleAuthorization);
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      return jsonResponse(200, { items: [], totalCount: 0, page: 1, pageSize: 1 });
    });
    vi.stubGlobal("fetch", fetchMock);
    window.history.replaceState({}, "", "/admin/organizations/not-a-guid/people");
    render(<App />);
    expect(
      await screen.findByRole("heading", { name: "Organization not found" }),
    ).toBeInTheDocument();
    expect(fetchMock.mock.calls.every(([input]) => !String(input).includes("/members"))).toBe(true);
    expect(fetchMock.mock.calls.every(([input]) => !String(input).includes("/invitations"))).toBe(
      true,
    );
  });
});
