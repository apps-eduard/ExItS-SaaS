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
  createdAtUtc: "2026-01-15T08:00:00Z",
  updatedAtUtc: "2026-08-01T08:00:00Z",
  profile: { legalName: "Northwind LLC" },
};

function stubDesktop() {
  vi.spyOn(window, "matchMedia").mockImplementation((query: string) => {
    return {
      matches: query.includes("min-width: 1024px") || query.includes("min-width: 768px"),
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

describe("organization commerce & compliance page", () => {
  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
  });

  it("renders Disabled payments and Not approved BIR with gated actions", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      onlineSupplierPayments: { status: "Disabled" },
      complianceStatus: { complianceEligibilityStatus: "NotRequested" },
    });
    window.history.replaceState(
      {},
      "",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/commerce-compliance",
    );
    render(<App />);

    expect(await screen.findByRole("heading", { name: "Commerce & Compliance" })).toBeInTheDocument();
    expect(await screen.findByText("Disabled")).toBeInTheDocument();
    expect(await screen.findByText("Not approved")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Enable" })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Suspend" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Restore review" })).toBeInTheDocument();
  });

  it("shows Enabled/Suspended payments actions and BIR approve when under review", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      onlineSupplierPayments: {
        status: "Available",
        updatedAtUtc: "2026-09-17T08:00:00Z",
        updatedByActorReference: "platform-admin",
        reason: "ready",
      },
      complianceStatus: {
        complianceEligibilityStatus: "UnderReview",
        updatedAtUtc: "2026-09-16T08:00:00Z",
        updatedByActorReference: "platform-admin",
      },
    });
    window.history.replaceState(
      {},
      "",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/commerce-compliance",
    );
    render(<App />);

    expect(await screen.findByText("Enabled")).toBeInTheDocument();
    expect(screen.getByText("Pending review")).toBeInTheDocument();
    expect(screen.getByText("ready")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Disable" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Suspend" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Approve" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Reject" })).toBeInTheDocument();
  });

  it("hides mutation actions without manage organizations permission", async () => {
    stubDesktop();
    const fetchMock = vi.fn(async (input: RequestInfo | URL) => {
      const url = String(input);
      if (url.includes("/auth/me")) {
        return jsonResponse(200, sampleSession);
      }
      if (url.includes("/authorization/me")) {
        return jsonResponse(200, {
          ...sampleAuthorization,
          permissions: sampleAuthorization.permissions.filter(
            (code) => code !== "platform.permission.manage_organizations",
          ),
        });
      }
      if (url.includes("/health")) {
        return textResponse(200, "Healthy");
      }
      if (url.includes("online-supplier-payments")) {
        return jsonResponse(200, {
          organizationId: sampleOrg.id,
          status: "Available",
        });
      }
      if (url.includes("compliance-status")) {
        return jsonResponse(200, {
          organizationId: sampleOrg.id,
          complianceEligibilityStatus: "UnderReview",
          taxDocumentIssuanceEnabled: false,
          taxDocumentIssuanceStatus: "NotEnabled",
          taxConfigurationEnabled: false,
          taxConfigurationStatus: "NotEnabled",
          taxDocumentImplementationAvailable: false,
          currentOwnerEducationAcknowledged: false,
          educationVersion: "v1",
        });
      }
      if (url.includes(`/organizations/${sampleOrg.id}`)) {
        return jsonResponse(200, sampleOrg);
      }
      return jsonResponse(404, { title: "Not Found", status: 404 });
    });
    vi.stubGlobal("fetch", fetchMock);

    window.history.replaceState(
      {},
      "",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/commerce-compliance",
    );
    render(<App />);

    expect(await screen.findByText("Enabled")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getAllByText(/managing them requires/i).length).toBeGreaterThan(0);
    });
    expect(screen.queryByRole("button", { name: "Disable" })).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Approve" })).not.toBeInTheDocument();
  });

  it("opens enable confirmation from the workspace nav", async () => {
    stubDesktop();
    mockAuthenticatedFetch({
      organizationItems: [sampleOrg],
      onlineSupplierPayments: { status: "Disabled" },
      complianceStatus: { complianceEligibilityStatus: "NotRequested" },
    });
    const user = userEvent.setup();
    window.history.replaceState(
      {},
      "",
      "/admin/organizations/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    );
    render(<App />);

    await user.click(await screen.findByRole("link", { name: "Commerce & Compliance" }));
    await waitFor(() => {
      expect(window.location.pathname).toContain("/commerce-compliance");
    });
    await user.click(await screen.findByRole("button", { name: "Enable" }));
    const dialog = await screen.findByRole("dialog");
    expect(within(dialog).getByText(/Enable online supplier payments/i)).toBeInTheDocument();
  });
});
