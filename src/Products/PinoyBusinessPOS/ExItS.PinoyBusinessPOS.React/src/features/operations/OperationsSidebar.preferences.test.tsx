import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter, Route, Routes, useLocation } from "react-router-dom";
import { OperationsSidebar } from "@/features/operations/OperationsSidebar";
import { resolvePreferencesReturnTo } from "@/features/preferences/preferences-return";

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({ t: (key: string) => key }),
}));

vi.mock("@/workspace/WorkspaceProvider", () => ({
  useWorkspace: () => ({
    boundWorkspace: {
      organizationId: "11111111-1111-1111-1111-111111111111",
      organizationDisplayName: "Test Org",
      branchId: "22222222-2222-2222-2222-222222222222",
      branchName: "Main",
      branchType: "Retail",
      experience: "operations",
    },
    sessionGrant: {
      productAccessAllowed: true,
      mappedPosRoleCode: "StoreManager",
      productLocalRoleCode: "StoreManager",
      canManageInventory: true,
      canViewReports: true,
      canCreateSale: true,
    },
  }),
}));

function LocationProbe() {
  const location = useLocation();
  return (
    <div data-testid="location-probe">
      {location.pathname}
      {location.state && typeof location.state === "object" && "returnTo" in location.state
        ? `|${String((location.state as { returnTo?: string }).returnTo)}`
        : ""}
    </div>
  );
}

describe("OperationsSidebar preferences return", () => {
  it("remembers the current content route when opening preferences", async () => {
    const user = userEvent.setup();
    render(
      <MemoryRouter initialEntries={["/inventory"]}>
        <Routes>
          <Route
            path="*"
            element={
              <>
                <OperationsSidebar />
                <LocationProbe />
              </>
            }
          />
        </Routes>
      </MemoryRouter>,
    );

    expect(screen.getByTestId("location-probe")).toHaveTextContent("/inventory");
    await user.click(screen.getByTestId("ops-sidebar-preferences"));
    expect(screen.getByTestId("location-probe")).toHaveTextContent(
      "/settings/preferences|/inventory",
    );
    expect(resolvePreferencesReturnTo(null, "/more")).toBe("/inventory");
  });
});
