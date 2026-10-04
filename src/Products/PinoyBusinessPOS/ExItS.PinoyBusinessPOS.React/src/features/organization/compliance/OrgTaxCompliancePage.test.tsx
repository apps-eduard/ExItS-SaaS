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
const BRANCH_ID = "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb";

const getOrganizationComplianceProfile = vi.fn();
const getComplianceActivationReadiness = vi.fn();
const listBranchComplianceProfiles = vi.fn();
const listComplianceRegistrationRecords = vi.fn();
const listOrganizationBranches = vi.fn();
const updateRegisteredTaxpayer = vi.fn();
const upsertBranchComplianceProfile = vi.fn();
const addComplianceRegistrationRecord = vi.fn();
const submitComplianceReadinessForReview = vi.fn();

let membershipRole = "OrganizationOwner";
let organizationManagementAuthority = true;
let organizationId: string | null = ORG_ID;

vi.mock("@/api/platform/organization-compliance-client", async () => {
  const actual = await vi.importActual<
    typeof import("@/api/platform/organization-compliance-client")
  >("@/api/platform/organization-compliance-client");
  return {
    ...actual,
    getOrganizationComplianceProfile: (...args: unknown[]) =>
      getOrganizationComplianceProfile(...args),
    getComplianceActivationReadiness: (...args: unknown[]) =>
      getComplianceActivationReadiness(...args),
    listBranchComplianceProfiles: (...args: unknown[]) => listBranchComplianceProfiles(...args),
    listComplianceRegistrationRecords: (...args: unknown[]) =>
      listComplianceRegistrationRecords(...args),
    listOrganizationBranches: (...args: unknown[]) => listOrganizationBranches(...args),
    updateRegisteredTaxpayer: (...args: unknown[]) => updateRegisteredTaxpayer(...args),
    upsertBranchComplianceProfile: (...args: unknown[]) => upsertBranchComplianceProfile(...args),
    addComplianceRegistrationRecord: (...args: unknown[]) =>
      addComplianceRegistrationRecord(...args),
    submitComplianceReadinessForReview: (...args: unknown[]) =>
      submitComplianceReadinessForReview(...args),
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

const { OrgTaxCompliancePage } = await import(
  "@/features/organization/compliance/OrgTaxCompliancePage"
);

function payload() {
  return {
    profile: {
      organizationId: ORG_ID,
      registeredTaxpayerName: "Mica Store",
      maskedTin: "***-***-789",
      tin: "123-456-789-000",
      setupStatus: "Configured",
      complianceEligibilityStatus: "InReview",
      taxConfigurationEnabled: false,
    },
    readiness: {
      organizationId: ORG_ID,
      overallStatus: "NeedsAttention",
      isReadyForTaxDocumentActivation: false,
      blockingReasons: ["Registered taxpayer name is required"],
      checklist: [{ code: "taxpayer", label: "Taxpayer name", done: true }],
    },
    branches: [{ id: BRANCH_ID, name: "Main", code: "MAIN", status: "Active" }],
    branchProfiles: [
      {
        organizationBranchId: BRANCH_ID,
        birBranchCode: "001",
        setupStatus: "SetupInProgress",
        notes: "Front counter",
      },
    ],
    registrations: [
      {
        id: "reg-1",
        registrationType: "PosPermitToUse",
        referenceNumber: "PTU-1",
        status: "Provided",
        recordedAtUtc: "2026-10-01T00:00:00.000Z",
      },
    ],
  };
}

function prime() {
  const data = payload();
  getOrganizationComplianceProfile.mockResolvedValue(data.profile);
  getComplianceActivationReadiness.mockResolvedValue(data.readiness);
  listOrganizationBranches.mockResolvedValue(data.branches);
  listBranchComplianceProfiles.mockResolvedValue(data.branchProfiles);
  listComplianceRegistrationRecords.mockResolvedValue(data.registrations);
}

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter>
            <OrgTaxCompliancePage />
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("OrgTaxCompliancePage", () => {
  beforeEach(() => {
    membershipRole = "OrganizationOwner";
    organizationManagementAuthority = true;
    organizationId = ORG_ID;
    vi.clearAllMocks();
    prime();
  });

  it("renders readiness, masked TIN, blockers, branches, and registrations", async () => {
    renderPage();
    expect(await screen.findByTestId("tax-overall-status")).toHaveTextContent("NeedsAttention");
    expect(screen.getByTestId("tax-progress")).toHaveTextContent("1 of 1");
    expect(screen.getByTestId("tax-masked-tin")).toHaveTextContent("***-***-789");
    expect(screen.getByTestId("tax-blocking")).toHaveTextContent("Registered taxpayer name is required");
    expect(screen.getByTestId("tax-branch-row")).toHaveTextContent("MAIN");
    expect(screen.getByTestId("tax-branch-row")).toHaveTextContent("001");
    expect(screen.getByTestId("tax-registration-row")).toHaveTextContent("PTU-1");
    expect(document.body.textContent).not.toContain("123-456-789-000");
    expect(screen.getByTestId("tax-issuance").textContent).not.toMatch(
      /BIR Certified|BIR Accredited|BIR Compliant/,
    );
    expect(screen.getByTestId("tax-sales-documents-link")).toHaveAttribute(
      "href",
      "/org/sales-documents",
    );
  });

  it("lets the Owner save taxpayer data, a branch, a registration, and readiness", async () => {
    const user = userEvent.setup();
    updateRegisteredTaxpayer.mockResolvedValue(payload().profile);
    upsertBranchComplianceProfile.mockResolvedValue(undefined);
    addComplianceRegistrationRecord.mockResolvedValue(undefined);
    submitComplianceReadinessForReview.mockResolvedValue(payload().readiness);
    renderPage();
    await screen.findByTestId("tax-save-taxpayer");
    await user.type(screen.getByTestId("tax-tin-input"), "123-456-789-000");
    await user.click(screen.getByTestId("tax-save-taxpayer"));
    await waitFor(() => expect(updateRegisteredTaxpayer).toHaveBeenCalled());
    expect(updateRegisteredTaxpayer.mock.calls[0]?.[0]).toBe(ORG_ID);

    await user.click(screen.getByTestId(`tax-edit-branch-${BRANCH_ID}`));
    await user.click(screen.getByTestId("tax-save-branch"));
    await waitFor(() =>
      expect(upsertBranchComplianceProfile).toHaveBeenCalledWith(
        ORG_ID,
        BRANCH_ID,
        expect.objectContaining({ birBranchCode: "001" }),
      ),
    );

    await user.click(screen.getByTestId("tax-add-registration"));
    await waitFor(() => expect(addComplianceRegistrationRecord).toHaveBeenCalled());

    await user.click(screen.getByTestId("tax-submit-readiness"));
    await waitFor(() => expect(submitComplianceReadinessForReview).toHaveBeenCalledWith(ORG_ID));
  });

  it("hides mutation controls from an organization administrator", async () => {
    membershipRole = "OrganizationAdministrator";
    renderPage();
    await screen.findByTestId("tax-masked-tin");
    expect(screen.queryByTestId("tax-save-taxpayer")).not.toBeInTheDocument();
    expect(screen.queryByTestId("tax-add-registration")).not.toBeInTheDocument();
    expect(screen.queryByTestId("tax-submit-readiness")).not.toBeInTheDocument();
  });

  it("denies a cashier and does not call the compliance API", async () => {
    membershipRole = "OrganizationMember";
    organizationManagementAuthority = false;
    renderPage();
    expect(await screen.findByTestId("tax-compliance-denied")).toBeInTheDocument();
    expect(getOrganizationComplianceProfile).not.toHaveBeenCalled();
  });

  it("shows a server rejection without echoing a secret", async () => {
    const user = userEvent.setup();
    updateRegisteredTaxpayer.mockRejectedValue(
      new PlatformApiError(400, { detail: "TIN could not be saved." }),
    );
    renderPage();
    await screen.findByTestId("tax-save-taxpayer");
    await user.click(screen.getByTestId("tax-save-taxpayer"));
    expect(await screen.findByTestId("tax-action-error")).toHaveTextContent(
      "TIN could not be saved.",
    );
  });

  it("shows a loading state until the profile arrives", () => {
    getOrganizationComplianceProfile.mockReturnValue(new Promise(() => undefined));
    renderPage();
    expect(screen.getByTestId("tax-loading")).toBeInTheDocument();
  });

  it("shows a load error and retries", async () => {
    const user = userEvent.setup();
    getOrganizationComplianceProfile.mockRejectedValueOnce(
      new PlatformApiError(500, { detail: "unavailable" }),
    );
    renderPage();
    expect(await screen.findByTestId("tax-load-error")).toBeInTheDocument();
    await user.click(screen.getByTestId("tax-retry"));
    await waitFor(() => expect(getOrganizationComplianceProfile.mock.calls.length).toBeGreaterThan(1));
  });

  it("does not introduce accreditation claims in the new copy", () => {
    for (const [key, value] of Object.entries(en)) {
      if (key.startsWith("orgTax.") || key.startsWith("admin.nav.taxCompliance")) {
        expect(value).not.toMatch(/BIR Certified|BIR Accredited|BIR Compliant/);
      }
    }
  });
});
