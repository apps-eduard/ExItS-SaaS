import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { App } from "@/app/App";
import { jsonResponse, mockAuthenticatedFetch } from "@/test/auth-fixtures";

const sampleRequirement: {
  id: string;
  code: string;
  title: string;
  category: string;
  description: string;
  requirementLevel: string;
  status: string;
  ownerRole: string;
  version: string;
  effectiveDate: string | null;
  lastReviewedDate: string | null;
  nextReviewDate: string | null;
  notes: string | null;
  sourceReference: string | null;
  requiresDpoLegalVerification: boolean;
  createdAtUtc: string;
  updatedAtUtc: string;
  evidenceCount: number;
} = {
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
  requirementsBody?: unknown;
  requirementDetail?: typeof sampleRequirement;
  patchStatus?: number;
  patchBody?: unknown;
}) {
  let currentRequirement = { ...(options?.requirementDetail ?? sampleRequirement) };
  const fetchMock = mockAuthenticatedFetch({
    permissions: options?.permissions ?? ["platform.permission.view_privacy_compliance"],
  });
  fetchMock.mockImplementation(async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const method = (init?.method ?? "GET").toUpperCase();
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
    if (url.includes("/antiforgery/token")) {
      return jsonResponse(200, { headerName: "X-XSRF-TOKEN", token: "test-antiforgery-token" });
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
    if (
      method === "PATCH" &&
      url.includes(`/privacy-compliance/requirements/${currentRequirement.id}`)
    ) {
      if ((options?.patchStatus ?? 200) >= 400) {
        return jsonResponse(
          options?.patchStatus ?? 500,
          options?.patchBody ?? { detail: "save failed", title: "Error", status: 500 },
        );
      }
      const body =
        typeof init?.body === "string" ? (JSON.parse(init.body) as Record<string, unknown>) : {};
      if (url.endsWith("/status") && typeof body.status === "string") {
        currentRequirement = { ...currentRequirement, status: body.status };
      } else if (typeof body.notes === "string" || body.notes === null) {
        currentRequirement = {
          ...currentRequirement,
          notes: typeof body.notes === "string" ? body.notes : null,
        };
      }
      return jsonResponse(200, currentRequirement);
    }
    if (url.includes(`/privacy-compliance/requirements/${currentRequirement.id}`)) {
      return jsonResponse(200, currentRequirement);
    }
    if (url.includes("/privacy-compliance/requirements")) {
      if ((options?.requirementsStatus ?? 200) >= 400) {
        return jsonResponse(
          options?.requirementsStatus ?? 500,
          options?.requirementsBody ?? { detail: "requirements failed", title: "Error", status: 500 },
        );
      }
      return jsonResponse(
        options?.requirementsStatus ?? 200,
        options?.requirements ?? [currentRequirement],
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
    const user = userEvent.setup();
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
    expect(await screen.findByTestId("privacy-overview-tab-category")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(screen.getByTestId("privacy-overview-panel-category")).toBeInTheDocument();
    expect(screen.queryByTestId("privacy-overview-panel-gaps")).not.toBeInTheDocument();

    await user.click(await screen.findByTestId("privacy-overview-tab-gaps"));
    expect(await screen.findByTestId("privacy-overview-panel-gaps")).toBeInTheDocument();
    expect(await screen.findByText(/No important gaps/i)).toBeInTheDocument();
    expect(screen.queryByText(/Unable to load/i)).not.toBeInTheDocument();
  });

  it("defaults to Category readiness and switches Privacy overview tabs", async () => {
    const user = userEvent.setup();
    mockPrivacyFetch({
      overviewBody: {
        ...sampleOverview,
        privacyImpactFollowUps: [
          {
            code: "PIA-CORE",
            title: "Core processing PIA",
            status: "ActionNeeded",
            requiresDpoLegalVerification: true,
            evidenceCount: 0,
          },
        ],
      },
      requirements: [sampleRequirement],
    });
    render(<App />);
    expect(await screen.findByTestId("privacy-overview-tabs")).toBeInTheDocument();

    const categoryTab = screen.getByTestId("privacy-overview-tab-category");
    const piaTab = screen.getByTestId("privacy-overview-tab-pia");
    const gapsTab = screen.getByTestId("privacy-overview-tab-gaps");

    expect(categoryTab).toHaveAttribute("aria-selected", "true");
    const categoryPanel = screen.getByTestId("privacy-overview-panel-category");
    expect(categoryPanel).toBeInTheDocument();
    expect(within(categoryPanel).getByText("Documents")).toBeInTheDocument();
    expect(screen.queryByText("Core processing PIA")).not.toBeInTheDocument();
    expect(screen.queryByText("Privacy Notice")).not.toBeInTheDocument();

    await user.click(piaTab);
    expect(piaTab).toHaveAttribute("aria-selected", "true");
    expect(categoryTab).toHaveAttribute("aria-selected", "false");
    expect(screen.getByTestId("privacy-overview-panel-pia")).toBeInTheDocument();
    expect(screen.getByText("Core processing PIA")).toBeInTheDocument();
    expect(screen.queryByTestId("privacy-overview-panel-category")).not.toBeInTheDocument();

    await user.click(gapsTab);
    expect(gapsTab).toHaveAttribute("aria-selected", "true");
    expect(screen.getByTestId("privacy-overview-panel-gaps")).toBeInTheDocument();
    expect(screen.getByText("Privacy Notice")).toBeInTheDocument();
    expect(screen.queryByText("Core processing PIA")).not.toBeInTheDocument();
  });

  it("supports keyboard navigation across Privacy overview tabs", async () => {
    const user = userEvent.setup();
    mockPrivacyFetch();
    render(<App />);
    expect(await screen.findByTestId("privacy-overview-tablist")).toBeInTheDocument();

    const categoryTab = screen.getByTestId("privacy-overview-tab-category");
    categoryTab.focus();
    expect(categoryTab).toHaveFocus();

    await user.keyboard("{ArrowRight}");
    expect(screen.getByTestId("privacy-overview-tab-pia")).toHaveAttribute("aria-selected", "true");
    expect(screen.getByTestId("privacy-overview-tab-pia")).toHaveFocus();

    await user.keyboard("{ArrowRight}");
    expect(screen.getByTestId("privacy-overview-tab-gaps")).toHaveAttribute("aria-selected", "true");
    expect(screen.getByTestId("privacy-overview-panel-gaps")).toBeInTheDocument();

    await user.keyboard("{Home}");
    expect(screen.getByTestId("privacy-overview-tab-category")).toHaveAttribute("aria-selected", "true");
  });

  it("preserves Important gaps loading and error states inside the gaps tab", async () => {
    const user = userEvent.setup();
    mockPrivacyFetch({
      requirementsStatus: 500,
      requirementsBody: { detail: "gaps failed", title: "Error", status: 500 },
    });
    render(<App />);
    expect(await screen.findByTestId("privacy-overview-tab-gaps")).toBeInTheDocument();
    await user.click(screen.getByTestId("privacy-overview-tab-gaps"));
    expect(await screen.findByText(/Unable to load important gap requirements/i)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /retry|try again|subukan/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /copy error details/i })).toBeInTheDocument();
  });

  it("keeps Privacy overview tablist horizontally scrollable on narrow viewports", async () => {
    Object.defineProperty(HTMLElement.prototype, "scrollWidth", {
      configurable: true,
      get() {
        return 640;
      },
    });
    Object.defineProperty(HTMLElement.prototype, "clientWidth", {
      configurable: true,
      get() {
        return 320;
      },
    });
    mockPrivacyFetch();
    render(<App />);
    const tablist = await screen.findByTestId("privacy-overview-tablist");
    expect(tablist.className).toMatch(/overflow-x-auto/);
    expect(tablist.scrollWidth).toBeGreaterThan(tablist.clientWidth);
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

  it("view-only users can inspect but cannot manage", async () => {
    const user = userEvent.setup();
    mockPrivacyFetch({
      permissions: ["platform.permission.view_privacy_compliance"],
      requirementDetail: { ...sampleRequirement, notes: "Existing note" },
    });
    window.history.replaceState({}, "", "/admin/privacy-compliance/documents");
    render(<App />);
    await user.click(await screen.findByRole("button", { name: /view details/i }));
    expect(await screen.findByTestId("privacy-requirement-drawer")).toBeInTheDocument();
    expect(await screen.findByText("Existing note")).toBeInTheDocument();
    expect(screen.getByTestId("privacy-requirement-pdf")).toBeInTheDocument();
    expect(screen.queryByTestId("privacy-requirement-manage")).not.toBeInTheDocument();
    expect(screen.queryByTestId("privacy-requirement-save")).not.toBeInTheDocument();
  });

  it("manage users can update status and notes with success feedback", async () => {
    const user = userEvent.setup();
    mockPrivacyFetch({
      permissions: [
        "platform.permission.view_privacy_compliance",
        "platform.permission.manage_privacy_compliance",
      ],
    });
    window.history.replaceState({}, "", "/admin/privacy-compliance/documents");
    render(<App />);
    await user.click(await screen.findByRole("button", { name: /view details/i }));
    expect(await screen.findByTestId("privacy-requirement-manage")).toBeInTheDocument();

    await user.selectOptions(screen.getByTestId("privacy-requirement-status"), "Approved");
    await user.clear(screen.getByTestId("privacy-requirement-notes"));
    await user.type(screen.getByTestId("privacy-requirement-notes"), "Reviewed by counsel");
    await user.click(screen.getByTestId("privacy-requirement-save"));

    expect(await screen.findByTestId("privacy-requirement-save-success")).toBeInTheDocument();
    expect(screen.getByText(/Requirement updated/i)).toBeInTheDocument();
    expect(screen.queryByTestId("privacy-requirement-save-error")).not.toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByTestId("privacy-requirement-status")).toHaveValue("Approved");
    });
  });

  it("failed save shows truthful error without success feedback", async () => {
    const user = userEvent.setup();
    mockPrivacyFetch({
      permissions: [
        "platform.permission.view_privacy_compliance",
        "platform.permission.manage_privacy_compliance",
      ],
      patchStatus: 500,
      patchBody: { detail: "upstream failure", title: "Error", status: 500 },
    });
    window.history.replaceState({}, "", "/admin/privacy-compliance/documents");
    render(<App />);
    await user.click(await screen.findByRole("button", { name: /view details/i }));
    await user.selectOptions(
      await screen.findByTestId("privacy-requirement-status"),
      "InProgress",
    );
    await user.click(screen.getByTestId("privacy-requirement-save"));

    expect(await screen.findByTestId("privacy-requirement-save-error")).toBeInTheDocument();
    expect(screen.getByText(/Could not update requirement/i)).toBeInTheDocument();
    expect(screen.getByText(/upstream failure/i)).toBeInTheDocument();
    expect(screen.queryByTestId("privacy-requirement-save-success")).not.toBeInTheDocument();
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
