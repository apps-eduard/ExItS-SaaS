import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { MemoryRouter } from "react-router-dom";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { BranchCreatePage } from "@/features/branches/BranchCreatePage";
import {
  BRANCH_DEFAULT_COUNTRY_CODE,
  BRANCH_DEFAULT_TIME_ZONE,
} from "@/features/branches/branch-defaults";

vi.mock("@/access/pos-capabilities", () => ({
  canManageBranchFulfillment: () => true,
  canInviteOrganizationStaff: () => true,
  canUseWarehouseBranches: () => false,
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

const pageSource = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "BranchCreatePage.tsx"),
  "utf8",
);
const globalsCss = readFileSync(
  resolve(dirname(fileURLToPath(import.meta.url)), "../../styles/globals.css"),
  "utf8",
);

describe("BranchCreatePage", () => {
  it("keeps PH and Asia/Manila read-only and suggests a branch code", async () => {
    const user = userEvent.setup();
    renderPage();

    expect(screen.getByTestId("branch-create-page")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-country")).toHaveAttribute("readonly");
    expect(screen.getByTestId("branch-create-country")).toHaveValue(BRANCH_DEFAULT_COUNTRY_CODE);
    expect(screen.getByTestId("branch-create-timezone")).toHaveAttribute("readonly");
    expect(screen.getByTestId("branch-create-timezone")).toHaveValue(BRANCH_DEFAULT_TIME_ZONE);
    expect(screen.getByTestId("branch-create-type")).toHaveValue("Retail");
    expect(screen.getByTestId("branch-create-phone")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-address1")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-address2")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-city")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-region")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-postal")).toBeInTheDocument();

    await user.type(screen.getByTestId("branch-create-name"), "East Branch");
    expect(screen.getByTestId("branch-create-code")).toHaveValue("EAST-BRANCH");
  });

  it("uses Branch details section and removes duplicate Add branch heading", () => {
    renderPage();

    expect(screen.getByTestId("branch-create-details")).toHaveTextContent(
      "branches.detailsTitle",
    );
    expect(screen.getByTestId("branch-create-address")).toHaveTextContent(
      "branches.addressTitle",
    );
    // PageHeader still uses create title; section must not reuse it.
    expect(pageSource).not.toMatch(
      /catalog-form-section__title[\s\S]{0,80}branches\.create\.title/,
    );
    expect(pageSource).toContain('t("branches.detailsTitle")');
  });

  it("shows compact warehouse entitlement and action icons", () => {
    renderPage();

    expect(screen.getByTestId("branch-create-warehouse-locked")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-cancel")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-submit")).toBeInTheDocument();
    expect(pageSource).toMatch(/<X[\s\S]*aria-hidden/);
    expect(pageSource).toMatch(/<Plus[\s\S]*aria-hidden/);
    expect(pageSource).toMatch(/LockKeyhole/);
  });

  it("consumes global density control height for catalog selects", () => {
    expect(globalsCss).toMatch(
      /\.catalog-form-select\s*\{[\s\S]*?min-height:\s*var\(--exits-control-height\)/,
    );
    expect(globalsCss).toContain(".branch-create-form");
    expect(globalsCss).toMatch(/\.branch-create-form[\s\S]*?max-width:\s*64rem/);
  });
});
