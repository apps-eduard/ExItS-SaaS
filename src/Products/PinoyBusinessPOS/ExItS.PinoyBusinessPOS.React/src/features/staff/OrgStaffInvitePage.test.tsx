import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import type { ReactNode } from "react";
import { MemoryRouter } from "react-router-dom";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { OrgStaffInvitePage } from "@/features/staff/OrgStaffInvitePage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

const resolveStaffInviteTarget = vi.fn();

vi.mock("@/api/platform/staff-invitation-client", () => ({
  resolveStaffInviteTarget: (...args: unknown[]) => resolveStaffInviteTarget(...args),
  createStaffInvitationByExItsId: vi.fn(),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "22222222-2222-4222-8222-222222222222",
      organizationDisplayName: "Corner Store",
      branchId: null,
      branchName: null,
    },
    sessionGrant: { membershipRole: "OrganizationOwner" },
    status: "ready",
  }),
  WorkspaceProvider: ({ children }: { children: ReactNode }) => children,
}));

function renderPage() {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter>
            <OrgStaffInvitePage />
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("OrgStaffInvitePage", () => {
  beforeEach(() => {
    resolveStaffInviteTarget.mockReset();
  });

  it("shows a found person who is already staff elsewhere and blocks the invite", async () => {
    resolveStaffInviteTarget.mockResolvedValue({
      ok: true,
      target: {
        publicUserId: "EX-1234-5678",
        displayName: "Maria Santos",
        userIdentityId: "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa",
        canInviteAsStaff: false,
      },
    });
    renderPage();

    await userEvent.type(screen.getByTestId("qr-manual-id"), "EX-1234-5678");
    await userEvent.click(screen.getByTestId("qr-manual-submit"));

    expect(await screen.findByText("Maria Santos")).toBeInTheDocument();
    expect(screen.getByText("EX-1234-5678")).toBeInTheDocument();
    expect(screen.getByTestId("staff-invite-already-staff")).toHaveTextContent(
      "This person is already staff of another organization and cannot be invited as staff.",
    );
    expect(screen.queryByRole("button", { name: "Continue" })).not.toBeInTheDocument();
  });

  it("lets the owner continue when the found person can be invited", async () => {
    resolveStaffInviteTarget.mockResolvedValue({
      ok: true,
      target: {
        publicUserId: "EX-2222-3333",
        displayName: "Ana Cruz",
        userIdentityId: "bbbbbbbb-bbbb-4bbb-8bbb-bbbbbbbbbbbb",
        canInviteAsStaff: true,
      },
    });
    renderPage();

    await userEvent.type(screen.getByTestId("qr-manual-id"), "EX-2222-3333");
    await userEvent.click(screen.getByTestId("qr-manual-submit"));

    expect(await screen.findByText("Ana Cruz")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Continue" })).toBeInTheDocument();
    expect(screen.queryByTestId("staff-invite-already-staff")).not.toBeInTheDocument();
  });
});
