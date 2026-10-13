import { beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import * as profileClient from "@/api/platform/membership-business-profile-client";
import * as membersClient from "@/api/platform/organization-members-client";
import { OrgStaffDetailPage } from "@/features/staff/OrgStaffDetailPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";
import { ToastProvider } from "@/components/exits/ToastProvider";

const authority = vi.hoisted(() => ({
  manage: true,
}));

vi.mock("@/api/platform/membership-business-profile-client", async (importOriginal) => {
  const actual = await importOriginal<typeof profileClient>();
  return {
    ...actual,
    getMembershipBusinessProfile: vi.fn(),
    updateMembershipBusinessProfile: vi.fn(),
  };
});

vi.mock("@/api/platform/organization-members-client", async (importOriginal) => {
  const actual = await importOriginal<typeof membersClient>();
  return {
    ...actual,
    listOrganizationMembers: vi.fn(),
  };
});

vi.mock("@/access/pos-capabilities", () => ({
  hasOrganizationManagementAuthority: () => authority.manage,
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    sessionGrant: {
      organizationId: "22222222-2222-4222-8222-222222222222",
    },
    boundWorkspace: {
      organizationId: "22222222-2222-4222-8222-222222222222",
    },
  }),
}));

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  const tree = (ui: ReactNode) => (
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <ToastProvider>
            <MemoryRouter initialEntries={["/org/staff/33333333-3333-4333-8333-333333333333"]}>
              <Routes>
                <Route path="/org/staff/:membershipId" element={ui} />
              </Routes>
            </MemoryRouter>
          </ToastProvider>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>
  );
  return render(tree(<OrgStaffDetailPage />));
}

describe("OrgStaffDetailPage", () => {
  beforeEach(() => {
    authority.manage = true;
    vi.mocked(profileClient.getMembershipBusinessProfile).mockResolvedValue({
      membershipId: "33333333-3333-4333-8333-333333333333",
      organizationId: "22222222-2222-4222-8222-222222222222",
      userId: "44444444-4444-4444-8444-444444444444",
      displayName: "Kizy Uy",
      role: "OrganizationMember",
      roleDisplay: "Staff",
      department: "Purchasing",
      jobTitle: "Purchasing Staff",
      workPhone: "+63917",
      workEmail: "kizy@example.com",
      isBusinessContact: true,
      staffId: "STF-0001",
      country: null,
      addressLine1: null,
      addressLine2: null,
      barangay: null,
      cityMunicipality: null,
      provinceState: null,
      postalCode: null,
      profileDetailsCaptured: false,
      firstName: null,
      middleName: null,
      lastName: null,
      dateOfBirth: null,
      gender: null,
      nationality: null,
      profilePhotoUrl: null,
      mobileNumber: null,
      email: null,
      staffDisplayName: null,
      personal: {
        firstName: "Kizy",
        middleName: null,
        lastName: "Uy",
        dateOfBirth: "1990-01-02",
        gender: "Female",
        nationality: "Filipino",
        profilePhotoUrl: null,
        mobileNumber: "+63917",
        email: "kizy@example.com",
        country: "Philippines",
        addressLine1: "12 Rizal Street",
        addressLine2: null,
        barangay: "Poblacion",
        cityMunicipality: "Makati",
        provinceState: "Metro Manila",
        postalCode: "1200",
      },
      updatedAtUtc: "2026-09-14T10:00:00Z",
    });
    vi.mocked(membersClient.listOrganizationMembers).mockResolvedValue({
      ok: true,
      members: [
        {
          id: "33333333-3333-4333-8333-333333333333",
          organizationId: "22222222-2222-4222-8222-222222222222",
          userId: "44444444-4444-4444-8444-444444444444",
          role: "OrganizationMember",
          status: "Active",
          department: "Purchasing",
          jobTitle: "Head Barista",
        },
      ],
    });
  });

  it("opens FormDrawer on the detail page for Owner/Admin without navigating", async () => {
    const user = userEvent.setup();
    renderPage();

    await screen.findByTestId("org-staff-detail-page");
    expect(screen.getByTestId("org-staff-tab-profile")).toHaveTextContent("Staff details");
    expect(screen.getByTestId("org-staff-tab-profile").querySelector("svg")).toBeTruthy();
    expect(screen.getByTestId("org-staff-tab-edit").querySelector("svg")).toBeTruthy();
    expect(screen.getByTestId("org-staff-tab-personal").querySelector("svg")).toBeTruthy();
    const header = screen.getByTestId("page-header-subtitle");
    expect(header).toHaveTextContent(/^Staff/);
    const role = header.querySelector(".text-primary");
    const manageAccess = screen.getByRole("link", { name: "Manage POS access" });
    expect(role).toHaveTextContent("Purchasing Staff");
    expect(header).toContainElement(manageAccess);
    expect(role!.compareDocumentPosition(manageAccess) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(screen.queryByTestId("member-bp-edit")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("org-staff-tab-edit"));

    expect(screen.getByTestId("member-business-profile-drawer")).toBeInTheDocument();
    const addressLine = screen.getByTestId("member-bp-address-line1");
    const postal = screen.getByTestId("member-bp-address-postal");
    expect(addressLine.compareDocumentPosition(postal) & Node.DOCUMENT_POSITION_FOLLOWING).toBeTruthy();
    expect(screen.getByTestId("member-bp-display-name")).toHaveValue("Kizy Uy");
    expect(screen.getByRole("heading", { name: "Kizy Uy" })).toBeInTheDocument();
    expect(screen.getByTestId("member-bp-department-trigger")).toHaveTextContent("Purchasing");
    expect(screen.getByTestId("member-bp-job-title-trigger")).toHaveTextContent("Purchasing Staff");
    expect(screen.getByTestId("org-staff-detail-page")).toBeInTheDocument();
  });

  it("shows the linked personal photo in the header on load", async () => {
    const profile = await vi.mocked(profileClient.getMembershipBusinessProfile)(
      "22222222-2222-4222-8222-222222222222",
      "33333333-3333-4333-8333-333333333333",
    );
    const photoUrl = "/api/v1/platform/organizations/22222222-2222-4222-8222-222222222222/members/33333333-3333-4333-8333-333333333333/personal-photo?v=1";
    vi.mocked(profileClient.getMembershipBusinessProfile).mockResolvedValue({
      ...profile,
      profileDetailsCaptured: true,
      profilePhotoUrl: null,
      personal: profile.personal ? { ...profile.personal, profilePhotoUrl: photoUrl } : null,
    });
    renderPage();
    const header = await screen.findByTestId("org-staff-header-photo");
    expect(header.querySelector("img")).toHaveAttribute("src", expect.stringContaining("personal-photo"));
  });

  it("shows the staff id and the linked personal profile", async () => {
    const user = userEvent.setup();
    renderPage();
    await screen.findByTestId("org-staff-detail-page");
    expect(screen.getByTestId("member-bp-staff-id-value")).toHaveTextContent("STF-0001");
    expect(screen.getByText("12 Rizal Street")).toBeInTheDocument();
    expect(screen.getByText("Poblacion")).toBeInTheDocument();
    expect(screen.getByText("1200")).toBeInTheDocument();
    await user.click(screen.getByTestId("org-staff-tab-personal"));
    expect(screen.getByTestId("org-staff-personal-panel")).toHaveTextContent("Filipino");
    expect(screen.getByTestId("org-staff-personal-panel")).toHaveTextContent("kizy@example.com");
    await user.click(screen.getByTestId("org-staff-tab-edit"));
    expect(screen.getByText(/Filled from the personal profile/)).toBeInTheDocument();
  });

  it("hides edit for normal Staff (read-only)", async () => {
    authority.manage = false;
    renderPage();
    await screen.findByTestId("org-staff-detail-page");
    expect(screen.queryByTestId("member-bp-edit")).not.toBeInTheDocument();
    expect(screen.queryByTestId("member-business-profile-drawer")).not.toBeInTheDocument();
  });

  it("keeps validation/error visible in the open drawer on save failure", async () => {
    const user = userEvent.setup();
    vi.mocked(profileClient.updateMembershipBusinessProfile).mockRejectedValue(
      new Error("Server rejected"),
    );
    renderPage();
    await screen.findByTestId("org-staff-detail-page");
    await user.click(screen.getByTestId("org-staff-tab-edit"));
    await user.click(screen.getByTestId("member-bp-is-business-contact"));
    await user.click(screen.getByTestId("member-bp-save"));
    await waitFor(() => expect(screen.getByTestId("member-bp-error")).toHaveTextContent("Server rejected"));
    expect(screen.getByTestId("member-business-profile-drawer")).toBeInTheDocument();
  });
});
