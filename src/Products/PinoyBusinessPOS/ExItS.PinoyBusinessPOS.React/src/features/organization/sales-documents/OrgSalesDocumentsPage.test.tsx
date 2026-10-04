import { beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { PlatformApiError } from "@/api/platform/platform-http";
import { I18nProvider } from "@/i18n/I18nProvider";
import { en } from "@/i18n/locales/en";
import { PreferencesProvider } from "@/hooks/usePreferences";

const ORG_ID = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";
const OTHER_ORG = "cccccccc-cccc-4ccc-8ccc-cccccccccccc";

const getSalesDocumentEducationStatus = vi.fn();
const acknowledgeSalesDocumentEducation = vi.fn();
const getOrganizationComplianceStatus = vi.fn();
const requestOrganizationComplianceReview = vi.fn();

let membershipRole = "OrganizationOwner";
let organizationManagementAuthority = true;
let organizationId: string | null = ORG_ID;

vi.mock("@/api/platform/organization-sales-document-education-client", async () => {
  const actual = await vi.importActual<
    typeof import("@/api/platform/organization-sales-document-education-client")
  >("@/api/platform/organization-sales-document-education-client");
  return {
    ...actual,
    getSalesDocumentEducationStatus: (...args: unknown[]) => getSalesDocumentEducationStatus(...args),
    acknowledgeSalesDocumentEducation: (...args: unknown[]) =>
      acknowledgeSalesDocumentEducation(...args),
  };
});

vi.mock("@/api/platform/organization-compliance-client", async () => {
  const actual = await vi.importActual<typeof import("@/api/platform/organization-compliance-client")>(
    "@/api/platform/organization-compliance-client",
  );
  return {
    ...actual,
    getOrganizationComplianceStatus: (...args: unknown[]) => getOrganizationComplianceStatus(...args),
    requestOrganizationComplianceReview: (...args: unknown[]) =>
      requestOrganizationComplianceReview(...args),
  };
});

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: organizationId
      ? { organizationId, organizationDisplayName: "Mica Store", experience: "manage_business" }
      : null,
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: membershipRole === "OrganizationOwner" ? "Owner" : "Cashier",
      productLocalRoleCode: "Owner",
      membershipRole,
      organizationManagementAuthority,
      featureCodes: [],
      grantedFeatureCodes: [],
    },
  }),
}));

const { OrgSalesDocumentsPage } = await import(
  "@/features/organization/sales-documents/OrgSalesDocumentsPage"
);

function education(overrides: Record<string, unknown> = {}) {
  return {
    organizationId: ORG_ID,
    currentVersion: "transaction-summary-v1",
    currentOwnerAcknowledged: false,
    acknowledgedAtUtc: null,
    requiresOwnerAction: true,
    transactionSummaryAvailable: true,
    taxDocumentIssuanceEnabled: false,
    documentMode: "TransactionSummary",
    ...overrides,
  };
}

function compliance(overrides: Record<string, unknown> = {}) {
  return {
    organizationId: ORG_ID,
    complianceEligibilityStatus: "NotRequested",
    taxDocumentIssuanceEnabled: false,
    taxDocumentIssuanceStatus: "NotEnabled",
    taxConfigurationEnabled: false,
    taxConfigurationStatus: "NotEnabled",
    taxDocumentImplementationAvailable: false,
    currentOwnerEducationAcknowledged: false,
    educationVersion: "transaction-summary-v1",
    ...overrides,
  };
}

function renderPage(path = "/org/sales-documents") {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={[path]}>
            <OrgSalesDocumentsPage />
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("OrgSalesDocumentsPage", () => {
  beforeEach(() => {
    membershipRole = "OrganizationOwner";
    organizationManagementAuthority = true;
    organizationId = ORG_ID;
    vi.clearAllMocks();
    getSalesDocumentEducationStatus.mockResolvedValue(education());
    getOrganizationComplianceStatus.mockResolvedValue(compliance());
  });

  it("lets an organization administrator read education without an acknowledgment action", async () => {
    membershipRole = "OrganizationAdministrator";
    renderPage(`/org/sales-documents?organizationId=${OTHER_ORG}`);
    expect(await screen.findByTestId("sales-education-status")).toHaveTextContent("Review needed");
    expect(screen.getByTestId("sales-document-mode")).toHaveTextContent("Transaction Summary");
    expect(screen.getByTestId("sales-disclaimer")).toHaveTextContent(
      "NOT A BIR INVOICE — FOR TRANSACTION REFERENCE ONLY",
    );
    expect(screen.getByTestId("sales-version")).toHaveTextContent("transaction-summary-v1");
    expect(screen.getByTestId("sales-issuance")).toHaveTextContent("not currently available");
    expect(screen.queryByTestId("sales-ack-checkbox")).not.toBeInTheDocument();
    expect(screen.queryByTestId("sales-request-review")).not.toBeInTheDocument();
    expect(getSalesDocumentEducationStatus.mock.calls[0]?.[0]).toBe(ORG_ID);
    expect(screen.getByTestId("sales-tax-link")).toHaveAttribute("href", "/org/tax-compliance");
    expect(document.body.textContent).not.toMatch(/BIR Certified|BIR Accredited|BIR Compliant|BIR Approved/);
  });

  it("renders an acknowledged timestamp without offering the checkbox again", async () => {
    getSalesDocumentEducationStatus.mockResolvedValue(
      education({
        currentOwnerAcknowledged: true,
        requiresOwnerAction: false,
        acknowledgedAtUtc: "2026-10-01T08:30:00.000Z",
      }),
    );
    renderPage();
    expect(await screen.findByTestId("sales-education-status")).toHaveTextContent("Acknowledged");
    expect(screen.getByTestId("sales-acknowledged-at")).toBeInTheDocument();
    expect(screen.queryByTestId("sales-ack-submit")).not.toBeInTheDocument();
  });

  it("requires the understanding checkbox before the Owner can acknowledge", async () => {
    const user = userEvent.setup();
    acknowledgeSalesDocumentEducation.mockResolvedValue(
      education({
        currentOwnerAcknowledged: true,
        requiresOwnerAction: false,
        acknowledgedAtUtc: "2026-10-04T09:00:00.000Z",
        taxDocumentIssuanceEnabled: false,
      }),
    );
    renderPage();
    const submit = await screen.findByTestId("sales-ack-submit");
    expect(submit).toBeDisabled();
    await user.click(screen.getByTestId("sales-ack-checkbox"));
    expect(submit).toBeEnabled();
    await user.click(submit);
    await waitFor(() => expect(acknowledgeSalesDocumentEducation).toHaveBeenCalledWith(ORG_ID));
    expect(await screen.findByTestId("sales-education-status")).toHaveTextContent("Acknowledged");
    expect(screen.getByTestId("sales-compliance-status")).toHaveTextContent("NotRequested");
    expect(screen.getByTestId("sales-issuance")).not.toHaveTextContent("Capability enabled");
  });

  it("keeps the unacknowledged state when acknowledgment is forbidden", async () => {
    const user = userEvent.setup();
    acknowledgeSalesDocumentEducation.mockRejectedValue(
      new PlatformApiError(403, { detail: "Only the current Owner may acknowledge." }),
    );
    renderPage();
    await user.click(await screen.findByTestId("sales-ack-checkbox"));
    await user.click(screen.getByTestId("sales-ack-submit"));
    expect(await screen.findByTestId("sales-action-error")).toHaveTextContent(
      "Only the current Owner may acknowledge.",
    );
    expect(screen.getByTestId("sales-education-status")).toHaveTextContent("Review needed");
  });

  it("lets the Owner request review and shows a safe rejection", async () => {
    const user = userEvent.setup();
    requestOrganizationComplianceReview.mockResolvedValueOnce(
      compliance({ complianceEligibilityStatus: "Requested", taxDocumentIssuanceEnabled: false }),
    );
    renderPage();
    await user.click(await screen.findByTestId("sales-request-review"));
    expect(await screen.findByTestId("sales-compliance-status")).toHaveTextContent("Requested");
    expect(screen.queryByTestId("sales-request-review")).not.toBeInTheDocument();
    expect(screen.getByTestId("sales-request-recorded")).toBeInTheDocument();
    expect(screen.getByTestId("sales-compliance-status")).not.toHaveTextContent("Approved");

    getOrganizationComplianceStatus.mockResolvedValue(
      compliance({ complianceEligibilityStatus: "Rejected" }),
    );
    requestOrganizationComplianceReview.mockRejectedValueOnce(
      new PlatformApiError(400, { detail: "Review could not be requested." }),
    );
    renderPage();
    await user.click(await screen.findByTestId("sales-request-review"));
    expect(await screen.findByTestId("sales-action-error")).toHaveTextContent(
      "Review could not be requested.",
    );
  });

  it("denies a cashier before calling the education API", async () => {
    membershipRole = "OrganizationMember";
    organizationManagementAuthority = false;
    renderPage();
    expect(await screen.findByTestId("sales-documents-denied")).toBeInTheDocument();
    expect(getSalesDocumentEducationStatus).not.toHaveBeenCalled();
  });

  it("does not introduce certification claims in the new copy", () => {
    for (const [key, value] of Object.entries(en)) {
      if (key.startsWith("orgSales.") || key === "admin.nav.salesDocuments") {
        expect(value).not.toMatch(/BIR Certified|BIR Accredited|BIR Compliant|BIR Approved/);
      }
    }
  });
});
