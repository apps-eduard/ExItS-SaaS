import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { AUTH_ERROR_CODES } from "@/api/auth/auth-types";
import {
  jsonResponse,
  mockAuthenticatedFetch,
  mockUnauthenticatedFetch,
  pagedJson,
} from "@/test/auth-fixtures";

const sampleAudit = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  occurredAtUtc: "2026-08-19T12:00:00Z",
  actorIdentifier: "olivia@example.test",
  actorType: "PlatformUser",
  actionCode: "platform.auth.signed_in",
  targetType: "AuthSession",
  targetId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
  organizationId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
  productCode: "POS",
  correlationId: "corr-1",
  outcome: "Succeeded",
  reason: null,
  summary: "Signed in",
};

describe("Platform Audit pages", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/admin/audit");
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("fails closed without view_audit_records", async () => {
    mockAuthenticatedFetch({
      permissions: ["platform.permission.view_portfolio"],
    });
    render(<App />);
    expect(await screen.findByRole("heading", { name: /not found/i })).toBeInTheDocument();
    expect(screen.queryByTestId("audit-list-page")).not.toBeInTheDocument();
  });

  it("shows empty success state without inventing rows", async () => {
    const fetchMock = mockAuthenticatedFetch();
    fetchMock.mockImplementation(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, {
          sessionId: "11111111-1111-1111-1111-111111111111",
          userId: "22222222-2222-2222-2222-222222222222",
          username: "olivia",
          displayName: "Olivia Mendoza",
          email: "olivia@example.test",
          expiresAtUtc: "2026-08-19T12:00:00Z",
          absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
          selectedOrganizationId: null,
          selectedOrganizationDisplayName: null,
          organizationSelectionState: "None",
          activeOrganizationCount: 0,
          accountClass: "Platform",
        });
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, {
          actorIdentifier: "olivia@example.test",
          actorType: "PlatformUser",
          platformUserId: "22222222-2222-2222-2222-222222222222",
          organizationId: null,
          permissions: ["platform.permission.view_audit_records"],
        });
      }
      if (url.includes("/api/v1/platform/audit?") || url.endsWith("/api/v1/platform/audit")) {
        return jsonResponse(200, pagedJson([], 0, 20));
      }
      return jsonResponse(404, {});
    });

    render(<App />);
    expect(await screen.findByTestId("audit-list-page")).toBeInTheDocument();
    expect(await screen.findByText("No audit records")).toBeInTheDocument();
    expect(screen.queryByText(sampleAudit.actionCode)).not.toBeInTheDocument();
  });

  it("renders audit rows, applies filters, and opens detail with organization link", async () => {
    const user = userEvent.setup();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, {
          sessionId: "11111111-1111-1111-1111-111111111111",
          userId: "22222222-2222-2222-2222-222222222222",
          username: "olivia",
          displayName: "Olivia Mendoza",
          email: "olivia@example.test",
          expiresAtUtc: "2026-08-19T12:00:00Z",
          absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
          selectedOrganizationId: null,
          selectedOrganizationDisplayName: null,
          organizationSelectionState: "None",
          activeOrganizationCount: 0,
          accountClass: "Platform",
        });
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, {
          actorIdentifier: "olivia@example.test",
          actorType: "PlatformUser",
          platformUserId: "22222222-2222-2222-2222-222222222222",
          organizationId: null,
          permissions: [
            "platform.permission.view_audit_records",
            "platform.permission.view_portfolio",
            "platform.permission.manage_organizations",
          ],
        });
      }
      if (url.includes(`/api/v1/platform/audit/${sampleAudit.id}`)) {
        return jsonResponse(200, sampleAudit);
      }
      if (url.includes("/api/v1/platform/audit")) {
        return jsonResponse(200, pagedJson([sampleAudit], 1, 20));
      }
      if (url.includes("/api/v1/platform/organizations/")) {
        return jsonResponse(200, {
          id: sampleAudit.organizationId,
          displayName: "Demo Org",
          status: "Active",
        });
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);

    render(<App />);
    expect(await screen.findByText(sampleAudit.actionCode)).toBeInTheDocument();
    expect(screen.getByTestId("audit-filters")).toBeInTheDocument();

    await user.type(screen.getByLabelText("Actor"), "olivia");
    await user.selectOptions(screen.getByLabelText("Outcome"), "Succeeded");
    await user.click(screen.getByRole("button", { name: "Apply filters" }));

    await waitFor(() => {
      expect(
        fetchMock.mock.calls.some(([request]) =>
          String(request).includes("actor=olivia") && String(request).includes("outcome=Succeeded"),
        ),
      ).toBe(true);
    });

    await user.click(screen.getByRole("link", { name: /19 Aug 2026|Aug 19/i }));
    expect(await screen.findByTestId("audit-detail-page")).toBeInTheDocument();
    expect(screen.getByText(sampleAudit.actorIdentifier)).toBeInTheDocument();
    expect(screen.getByText(sampleAudit.correlationId!)).toBeInTheDocument();
    const orgLink = screen.getByRole("link", { name: sampleAudit.organizationId! });
    expect(orgLink).toHaveAttribute(
      "href",
      `/admin/organizations/${sampleAudit.organizationId}`,
    );
  });

  it("shows ErrorState on API failure instead of an empty ready table", async () => {
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, {
          sessionId: "11111111-1111-1111-1111-111111111111",
          userId: "22222222-2222-2222-2222-222222222222",
          username: "olivia",
          displayName: "Olivia Mendoza",
          email: "olivia@example.test",
          expiresAtUtc: "2026-08-19T12:00:00Z",
          absoluteExpiresAtUtc: "2026-08-20T12:00:00Z",
          selectedOrganizationId: null,
          selectedOrganizationDisplayName: null,
          organizationSelectionState: "None",
          activeOrganizationCount: 0,
          accountClass: "Platform",
        });
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, {
          actorIdentifier: "olivia@example.test",
          actorType: "PlatformUser",
          platformUserId: "22222222-2222-2222-2222-222222222222",
          organizationId: null,
          permissions: ["platform.permission.view_audit_records"],
        });
      }
      if (url.includes("/api/v1/platform/audit")) {
        return jsonResponse(503, {
          status: 503,
          title: "Service Unavailable",
          errorCode: "platform.unavailable",
        });
      }
      return jsonResponse(404, {});
    });
    vi.stubGlobal("fetch", fetchMock);

    render(<App />);
    expect(await screen.findByText("Unable to load platform audit.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /retry/i })).toBeInTheDocument();
    expect(screen.queryByText("No audit records")).not.toBeInTheDocument();
  });

  it("does not treat unauthenticated access as an empty audit page", async () => {
    mockUnauthenticatedFetch();
    render(<App />);
    expect(await screen.findByRole("heading", { name: "Sign In" })).toBeInTheDocument();
    expect(screen.queryByTestId("audit-list-page")).not.toBeInTheDocument();
    expect(AUTH_ERROR_CODES.sessionInvalid).toBeTruthy();
  });
});
