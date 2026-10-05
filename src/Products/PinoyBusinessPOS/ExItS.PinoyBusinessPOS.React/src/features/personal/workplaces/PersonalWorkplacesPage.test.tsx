import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import * as workplacesClient from "@/api/platform/personal-workplaces-client";
import * as inviteClient from "@/api/platform/staff-invitation-client";
import { PersonalWorkplacesPage } from "@/features/personal/workplaces/PersonalWorkplacesPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

vi.mock("@/api/platform/personal-workplaces-client", async (importOriginal) => {
  const actual = await importOriginal<typeof workplacesClient>();
  return {
    ...actual,
    listPersonalWorkplaces: vi.fn(),
  };
});

vi.mock("@/api/platform/staff-invitation-client", async (importOriginal) => {
  const actual = await importOriginal<typeof inviteClient>();
  return {
    ...actual,
    listMyPendingStaffInvitations: vi.fn(),
    acceptStaffInvitationById: vi.fn(),
    declineStaffInvitationById: vi.fn(),
  };
});

const signIn = vi.fn(async () => ({ ok: true as const }));

vi.mock("@/connectivity/browser-online", () => ({
  useBrowserOnline: () => true,
}));

vi.mock("@/session/SessionProvider", () => ({
  useSession: () => ({
    session: {
      userId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
      email: "kizy@gmail.com",
      accountClass: "Personal",
    },
    status: "authenticated",
    signIn,
  }),
}));

const membershipId = "11111111-1111-4111-8111-111111111111";
const pendingId = "22222222-2222-4222-8222-222222222222";
const orgId = "33333333-3333-4333-8333-333333333333";

function renderPage(initial = "/personal/workplaces") {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={[initial]}>
            <Routes>
              <Route path="/personal/workplaces" element={<PersonalWorkplacesPage />} />
              <Route path="/switching-context" element={<div data-testid="switching-context" />} />
              <Route path="/" element={<div data-testid="post-login-home" />} />
              <Route path="/sign-in" element={<div data-testid="sign-in-page" />} />
            </Routes>
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("PersonalWorkplacesPage", () => {
  beforeEach(() => {
    signIn.mockReset();
    signIn.mockResolvedValue({ ok: true });
    vi.mocked(workplacesClient.listPersonalWorkplaces).mockResolvedValue({
      ok: true,
      workplaces: [
        {
          organizationId: orgId,
          organizationDisplayName: "Mica Store",
          publicOrganizationId: "ORG012345",
          staffUserId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          staffLogin: "kizy@ORG012345",
          membershipId,
          membershipRole: "OrganizationMember",
          membershipRoleDisplay: "Staff",
          membershipStatus: "Active",
          productRole: "Cashier",
          productRoleDisplay: "Cashier",
          branches: [
            {
              branchId: "44444444-4444-4444-8444-444444444444",
              name: "Kalibo Branch",
              code: "KALIBO",
              isPrimary: true,
            },
          ],
        },
      ],
    });
    vi.mocked(inviteClient.listMyPendingStaffInvitations).mockResolvedValue([
      {
        id: pendingId,
        organizationId: "55555555-5555-4555-8555-555555555555",
        email: "kizy@gmail.com",
        role: "OrganizationMember",
        status: "Pending",
        organizationDisplayName: "North Shop",
        productRole: "Manager",
        productRoleDisplay: "Manager",
      },
    ]);
  });

  afterEach(() => {
    vi.clearAllMocks();
  });

  it("shows workplaces and pending invitations with distinguished personal/work logins", async () => {
    renderPage();

    const workplace = await screen.findByTestId(`personal-workplace-${membershipId}`);
    expect(within(workplace).getByText("Mica Store")).toBeInTheDocument();
    expect(within(workplace).getByText("Active staff")).toBeInTheDocument();
    expect(within(workplace).getByText(/Role:\s*Cashier/)).toBeInTheDocument();
    expect(within(workplace).getByText(/Branch:\s*Kalibo Branch/)).toBeInTheDocument();
    expect(within(workplace).getByText("kizy@ORG012345")).toBeInTheDocument();
    expect(screen.getByTestId("personal-workplaces-personal-email")).toHaveTextContent(
      "kizy@gmail.com",
    );
    expect(screen.getByTestId(`personal-workplaces-pending-${pendingId}`)).toHaveTextContent(
      "North Shop",
    );
  });

  it("signs in with the workplace password on this page", async () => {
    const user = userEvent.setup();
    renderPage();

    await screen.findByTestId(`personal-workplace-${membershipId}`);
    await user.type(
      screen.getByTestId(`personal-workplace-password-${membershipId}`),
      "workplace-secret",
    );
    await user.click(screen.getByTestId(`personal-workplace-sign-in-${membershipId}`));
    expect(signIn).toHaveBeenCalledWith("kizy@ORG012345", "workplace-secret");
    expect(await screen.findByTestId("post-login-home")).toBeInTheDocument();
    expect(screen.queryByTestId(`personal-workplace-copy-${membershipId}`)).not.toBeInTheDocument();
  });

  it("does not label suspended membership as Active staff", async () => {
    vi.mocked(workplacesClient.listPersonalWorkplaces).mockResolvedValue({
      ok: true,
      workplaces: [
        {
          organizationId: orgId,
          organizationDisplayName: "Mica Store",
          publicOrganizationId: "ORG012345",
          staffUserId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
          staffLogin: "kizy@ORG012345",
          membershipId,
          membershipRole: "OrganizationMember",
          membershipRoleDisplay: "Staff",
          membershipStatus: "Suspended",
          productRole: "Cashier",
          productRoleDisplay: "Cashier",
          branches: [],
        },
      ],
    });

    renderPage();
    const workplace = await screen.findByTestId(`personal-workplace-${membershipId}`);
    expect(within(workplace).getByText("Suspended")).toBeInTheDocument();
    expect(within(workplace).queryByText("Active staff")).not.toBeInTheDocument();
  });
});
