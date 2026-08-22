import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { jsonResponse, mockAuthenticatedFetch } from "@/test/auth-fixtures";

const sampleRequirement = {
  id: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  code: "DOC-1",
  title: "Privacy Notice",
  category: "CustomerFacing",
  description: "Customer-facing privacy notice",
  requirementLevel: "Required",
  status: "NotStarted",
  ownerRole: "Platform",
  version: "1.0",
  effectiveDate: null,
  lastReviewedDate: null,
  nextReviewDate: null,
  notes: null,
  sourceReference: null,
  requiresDpoLegalVerification: false,
  createdAtUtc: "2026-01-01T00:00:00Z",
  updatedAtUtc: "2026-01-01T00:00:00Z",
  evidenceCount: 0,
};

const sampleOverview = {
  totalRequirements: 1,
  totalSystems: 0,
  totalEvidence: 0,
  requirementsByStatus: { NotStarted: 1 },
  requirementsByCategory: { CustomerFacing: 1 },
  lastUpdatedUtc: "2026-08-19T12:00:00Z",
  overallReadiness: "ActionNeeded",
  readyCount: 0,
  actionNeededCount: 1,
  externalLegalReviewCount: 0,
  requirementsWithEvidenceCount: 0,
  technicalSafeguardsSummary: "Partial",
  governanceDocumentationSummary: "Unavailable",
  legalReviewSummary: "Required",
  npcVerificationSummary: "NotVerified",
  categorySummaries: [
    {
      group: "Documents",
      detailRoute: "/admin/privacy-compliance/documents",
      requirementCount: 1,
      readyCount: 0,
      actionNeededCount: 1,
      evidenceCoveredCount: 0,
      lastReviewedDate: null,
      status: "ActionNeeded",
      hasActionNeeded: true,
    },
  ],
  privacyImpactFollowUps: [],
};

function mockPrivacyFetch(options?: {
  permissions?: string[];
  overviewStatus?: number;
  overviewBody?: unknown;
  requirements?: unknown[];
  requirementsStatus?: number;
}) {
  const fetchMock = mockAuthenticatedFetch({
    permissions: options?.permissions ?? ["platform.permission.view_privacy_compliance"],
  });
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
        permissions: options?.permissions ?? ["platform.permission.view_privacy_compliance"],
      });
    }
    if (url.includes("/privacy-compliance/overview")) {
      return jsonResponse(
        options?.overviewStatus ?? 200,
        options?.overviewBody ?? sampleOverview,
      );
    }
    if (url.includes("/privacy-compliance/requirements/") && url.includes("/evidence")) {
      return jsonResponse(200, []);
    }
    if (url.includes("/privacy-compliance/requirements/") && !url.includes("?")) {
      return jsonResponse(200, sampleRequirement);
    }
    if (url.includes("/privacy-compliance/requirements")) {
      return jsonResponse(
        options?.requirementsStatus ?? 200,
        options?.requirements ?? [sampleRequirement],
      );
    }
    if (url.includes("/privacy-compliance/systems")) {
      return jsonResponse(200, []);
    }
    return jsonResponse(404, {});
  });
  return fetchMock;
}

describe("Privacy Compliance pages", () => {
  beforeEach(() => {
    window.history.replaceState({}, "", "/admin/privacy-compliance");
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("fails closed without view_privacy_compliance", async () => {
    mockPrivacyFetch({ permissions: ["platform.permission.view_portfolio"] });
    render(<App />);
    expect(await screen.findByRole("heading", { name: /not found/i })).toBeInTheDocument();
    expect(screen.queryByTestId("privacy-overview-page")).not.toBeInTheDocument();
  });

  it("shows empty gaps from authoritative empty list, not invented ready state", async () => {
    mockPrivacyFetch({
      overviewBody: { ...sampleOverview, actionNeededCount: 0, requirementsByStatus: {} },
      requirements: [
        {
          ...sampleRequirement,
          status: "Approved",
          requirementLevel: "Required",
        },
      ],
    });
    render(<App />);
    expect(await screen.findByTestId("privacy-overview-page")).toBeInTheDocument();
    expect(await screen.findByTestId("privacy-disclaimer")).toBeInTheDocument();
    expect(screen.getByText(/Readiness tooling only/i)).toBeInTheDocument();
    expect(await screen.findByText(/No important gaps/i)).toBeInTheDocument();
    expect(screen.queryByText(/Unable to load/i)).not.toBeInTheDocument();
  });

  it("shows ErrorState with retry when overview fails", async () => {
    mockPrivacyFetch({
      overviewStatus: 500,
      overviewBody: { detail: "overview failed", title: "Error", status: 500 },
    });
    render(<App />);
    expect(await screen.findByText(/Unable to load privacy compliance overview/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /retry|try again|subukan/i })).toBeInTheDocument();
    expect(screen.queryByText(/^0$/)).not.toBeInTheDocument();
  });

  it("documents page filters and opens requirement drawer", async () => {
    const user = userEvent.setup();
    mockPrivacyFetch();
    window.history.replaceState({}, "", "/admin/privacy-compliance/documents");
    render(<App />);
    expect(await screen.findByTestId("privacy-documents-page")).toBeInTheDocument();
    expect(await screen.findByText("DOC-1")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: /view details/i }));
    expect(await screen.findByTestId("privacy-requirement-drawer")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByText("Customer-facing privacy notice")).toBeInTheDocument();
    });
  });

  it("PIA category route uses existing filter semantics", async () => {
    mockPrivacyFetch({
      requirements: [
        { ...sampleRequirement, id: "1", code: "PIA-1", category: "Internal", title: "Core PIA" },
        {
          ...sampleRequirement,
          id: "2",
          code: "DOC-9",
          category: "CustomerFacing",
          title: "Notice",
        },
      ],
    });
    window.history.replaceState({}, "", "/admin/privacy-compliance/pias");
    render(<App />);
    expect(await screen.findByTestId("privacy-category-pias-page")).toBeInTheDocument();
    expect(await screen.findByText("Core PIA")).toBeInTheDocument();
    expect(screen.queryByText("Notice")).not.toBeInTheDocument();
  });
});
