import { describe, expect, it, vi } from "vitest";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen } from "@testing-library/react";
import type { ReactNode } from "react";
import { MemoryRouter, Route, Routes } from "react-router-dom";
import * as definitionsClient from "@/api/platform/product-local-role-definitions-client";
import { OrgRolesPage } from "@/features/staff/OrgRolesPage";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

vi.mock("@/api/platform/product-local-role-definitions-client", () => ({
  listProductLocalRoleDefinitions: vi.fn(),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "22222222-2222-4222-8222-222222222222",
      organizationDisplayName: "Corner Store",
      branchId: null,
      branchName: null,
    },
    status: "ready",
  }),
  WorkspaceProvider: ({ children }: { children: ReactNode }) => children,
}));

const mockRoles = [
  {
    code: "Owner",
    displayName: "POS Owner",
    description: "Full operational access to Pinoy Business POS.",
    sortOrder: 1,
    isSystemRole: true,
    isAssignable: true,
    mappedPosRoleCode: "Owner",
    activeStaffCount: 1,
    permissionGroups: [],
  },
  {
    code: "Manager",
    displayName: "Manager",
    description: "Runs day-to-day store operations.",
    sortOrder: 2,
    isSystemRole: true,
    isAssignable: true,
    mappedPosRoleCode: "StoreManager",
    activeStaffCount: 2,
    permissionGroups: [],
  },
  {
    code: "Cashier",
    displayName: "Cashier",
    description: "Sells products and handles checkout.",
    sortOrder: 3,
    isSystemRole: true,
    isAssignable: true,
    mappedPosRoleCode: "Cashier",
    activeStaffCount: 4,
    permissionGroups: [],
  },
  {
    code: "InventoryStaff",
    displayName: "Inventory Staff",
    description: "Handles stock, purchasing, and inventory operations.",
    sortOrder: 4,
    isSystemRole: true,
    isAssignable: true,
    mappedPosRoleCode: "InventoryStaff",
    activeStaffCount: 1,
    permissionGroups: [],
  },
  {
    code: "ReportingUser",
    displayName: "Reporting User",
    description: "Views reports and business information without operational changes.",
    sortOrder: 5,
    isSystemRole: true,
    isAssignable: true,
    mappedPosRoleCode: "ReportingUser",
    activeStaffCount: 1,
    permissionGroups: [],
  },
];

function renderPage() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  });
  return render(
    <QueryClientProvider client={client}>
      <PreferencesProvider>
        <I18nProvider>
          <MemoryRouter initialEntries={["/org/roles"]}>
            <Routes>
              <Route path="/org/roles" element={<OrgRolesPage />} />
            </Routes>
          </MemoryRouter>
        </I18nProvider>
      </PreferencesProvider>
    </QueryClientProvider>,
  );
}

describe("OrgRolesPage", () => {
  it("renders five system roles from the server catalog", async () => {
    vi.mocked(definitionsClient.listProductLocalRoleDefinitions).mockResolvedValue({
      ok: true,
      roles: mockRoles,
    });

    renderPage();

    expect(await screen.findByTestId("org-roles-list")).toBeInTheDocument();
    expect(screen.getByText("POS Owner")).toBeInTheDocument();
    expect(screen.getByText("Manager")).toBeInTheDocument();
    expect(screen.getByText("Cashier")).toBeInTheDocument();
    expect(screen.getByText("Inventory Staff")).toBeInTheDocument();
    expect(screen.getByText("Reporting User")).toBeInTheDocument();
    expect(screen.getAllByText("System role")).toHaveLength(5);
    expect(screen.getByText("Custom roles are not available yet.")).toBeInTheDocument();
  });
});
