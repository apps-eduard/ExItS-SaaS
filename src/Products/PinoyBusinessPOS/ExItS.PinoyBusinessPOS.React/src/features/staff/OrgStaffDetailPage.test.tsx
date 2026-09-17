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
    expect(screen.getByText("Staff")).toBeInTheDocument();
    await user.click(screen.getByTestId("member-bp-edit"));

    expect(screen.getByTestId("member-business-profile-drawer")).toBeInTheDocument();
    expect(screen.getAllByText("Kizy Uy").length).toBeGreaterThanOrEqual(2);
    expect(screen.getByTestId("member-bp-department-trigger")).toHaveTextContent("Purchasing");
    expect(screen.getByTestId("member-bp-job-title-trigger")).toHaveTextContent("Purchasing Staff");
    expect(screen.getByTestId("org-staff-detail-page")).toBeInTheDocument();
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
    await user.click(screen.getByTestId("member-bp-edit"));
    await user.click(screen.getByTestId("member-bp-is-business-contact"));
    await user.click(screen.getByTestId("member-bp-save"));
    await waitFor(() => expect(screen.getByTestId("member-bp-error")).toHaveTextContent("Server rejected"));
    expect(screen.getByTestId("member-business-profile-drawer")).toBeInTheDocument();
  });
});
