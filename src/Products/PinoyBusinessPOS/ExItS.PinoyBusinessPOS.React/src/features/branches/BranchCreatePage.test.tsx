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

const canUseWarehouseBranches = vi.fn(() => false);

vi.mock("@/access/pos-capabilities", () => ({
  canManageBranchFulfillment: () => true,
  canInviteOrganizationStaff: () => true,
  canUseWarehouseBranches: () => canUseWarehouseBranches(),
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

function renderPage(path = "/org/branches/new") {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return render(
    <QueryClientProvider client={queryClient}>
      <MemoryRouter initialEntries={[path]}>
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
    canUseWarehouseBranches.mockReturnValue(false);
    const user = userEvent.setup();
    renderPage();

    expect(screen.getByTestId("branch-create-page")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-country")).toHaveAttribute("readonly");
    expect(screen.getByTestId("branch-create-country")).toHaveValue(BRANCH_DEFAULT_COUNTRY_CODE);
    expect(screen.getByTestId("branch-create-timezone")).toHaveAttribute("readonly");
    expect(screen.getByTestId("branch-create-timezone")).toHaveValue(BRANCH_DEFAULT_TIME_ZONE);
    expect(screen.getByTestId("branch-create-type-retail")).toBeInTheDocument();
    expect(screen.queryByTestId("branch-create-type-warehouse")).not.toBeInTheDocument();
    expect(screen.getByTestId("branch-create-phone")).toBeInTheDocument();

    await user.type(screen.getByTestId("branch-create-name"), "East Branch");
    expect(screen.getByTestId("branch-create-code")).toHaveValue("EAST-BRANCH");
  });

  it("preselects Retail from type=retail and uses retail copy", () => {
    canUseWarehouseBranches.mockReturnValue(true);
    renderPage("/org/branches/new?type=retail");
    expect(screen.getByTestId("branch-create-page")).toHaveAttribute("data-branch-type", "Retail");
    expect(screen.getByRole("heading", { name: "branches.create.title.retail" })).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-details")).toHaveTextContent(
      "branches.create.details.retail",
    );
    expect(screen.getByTestId("branch-create-submit")).toHaveTextContent(
      "branches.create.submit.retail",
    );
  });

  it("preselects Warehouse from type=warehouse when entitled", () => {
    canUseWarehouseBranches.mockReturnValue(true);
    renderPage("/org/branches/new?type=warehouse");
    expect(screen.getByTestId("branch-create-page")).toHaveAttribute(
      "data-branch-type",
      "Warehouse",
    );
    expect(
      screen.getByRole("heading", { name: "branches.create.title.warehouse" }),
    ).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-details")).toHaveTextContent(
      "branches.create.details.warehouse",
    );
    expect(screen.getByTestId("branch-create-submit")).toHaveTextContent(
      "branches.create.submit.warehouse",
    );
    expect(screen.getByText("branches.type.warehouseHelp")).toBeInTheDocument();
  });

  it("falls back to Retail when warehouse type requested without entitlement", () => {
    canUseWarehouseBranches.mockReturnValue(false);
    renderPage("/org/branches/new?type=warehouse");
    expect(screen.getByTestId("branch-create-page")).toHaveAttribute("data-branch-type", "Retail");
    expect(screen.getByTestId("branch-create-warehouse-locked")).toBeInTheDocument();
  });

  it("uses type-specific details section and removes duplicate create title heading", () => {
    canUseWarehouseBranches.mockReturnValue(false);
    renderPage();

    expect(screen.getByTestId("branch-create-details")).toHaveTextContent(
      "branches.create.details.retail",
    );
    expect(screen.getByTestId("branch-create-address")).toHaveTextContent(
      "branches.addressTitle",
    );
    expect(pageSource).not.toMatch(
      /catalog-form-section__title[\s\S]{0,80}branches\.create\.title/,
    );
  });

  it("shows compact warehouse entitlement and action icons", () => {
    canUseWarehouseBranches.mockReturnValue(false);
    renderPage();

    expect(screen.getByTestId("branch-create-warehouse-locked")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-submit")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-cancel")).toBeInTheDocument();
    expect(screen.getByTestId("branch-create-reset")).toBeInTheDocument();
    expect(pageSource).not.toContain("branches.create.codeHelper");
    expect(pageSource).toMatch(/<Plus[\s\S]*aria-hidden/);
    expect(pageSource).toMatch(/<X[\s\S]*aria-hidden/);
    expect(pageSource).toMatch(/<RotateCcw[\s\S]*aria-hidden/);
    expect(pageSource).toMatch(/LockKeyhole/);
  });

  it("resets form fields to defaults", async () => {
    canUseWarehouseBranches.mockReturnValue(false);
    const user = userEvent.setup();
    renderPage();

    await user.type(screen.getByTestId("branch-create-name"), "East Branch");
    await user.type(screen.getByTestId("branch-create-phone"), "09171234567");
    await user.type(screen.getByTestId("branch-create-city"), "Iloilo");
    expect(screen.getByTestId("branch-create-code")).toHaveValue("EAST-BRANCH");

    await user.click(screen.getByTestId("branch-create-reset"));

    expect(screen.getByTestId("branch-create-name")).toHaveValue("");
    expect(screen.getByTestId("branch-create-code")).toHaveValue("");
    expect(screen.getByTestId("branch-create-phone")).toHaveValue("");
    expect(screen.getByTestId("branch-create-city")).toHaveValue("");
    expect(screen.getByTestId("branch-create-type-retail")).toBeInTheDocument();
  });

  it("keeps branch create form width constraint in globals", () => {
    expect(globalsCss).toContain(".branch-create-form");
    expect(globalsCss).toMatch(/\.branch-create-form[\s\S]*?max-width:\s*64rem/);
  });
});
