import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { BranchCreatePage } from "@/features/branches/BranchCreatePage";
import { BRANCH_DEFAULT_COUNTRY_CODE, BRANCH_DEFAULT_TIME_ZONE } from "@/features/branches/branch-defaults";

vi.mock("@/access/pos-capabilities", () => ({
  canManageBranchFulfillment: () => true,
  canInviteOrganizationStaff: () => true,
}));

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: { organizationId: "11111111-1111-1111-1111-111111111111" },
    sessionGrant: { productRole: "Owner", organizationManagementAuthority: true },
  }),
}));

vi.mock("@/api/platform/organization-branches-client", () => ({
  createOrganizationBranch: vi.fn(),
}));

function renderPage() {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter>
        <BranchCreatePage />
      </MemoryRouter>
    </QueryClientProvider>,
  );
}

describe("BranchCreatePage", () => {
  it("keeps PH and Asia/Manila read-only and suggests a branch code", async () => {
    const user = userEvent.setup();
    renderPage();

    expect(screen.getByTestId("branch-create-country")).toHaveAttribute("readonly");
    expect(screen.getByTestId("branch-create-country")).toHaveValue(BRANCH_DEFAULT_COUNTRY_CODE);
    expect(screen.getByTestId("branch-create-timezone")).toHaveAttribute("readonly");
    expect(screen.getByTestId("branch-create-timezone")).toHaveValue(BRANCH_DEFAULT_TIME_ZONE);

    await user.type(screen.getByTestId("branch-create-name"), "East Branch");
    expect(screen.getByTestId("branch-create-code")).toHaveValue("EAST-BRANCH");
  });
});
