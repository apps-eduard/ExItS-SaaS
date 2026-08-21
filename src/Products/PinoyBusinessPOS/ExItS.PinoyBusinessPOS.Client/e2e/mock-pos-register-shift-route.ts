import type { Page } from "@playwright/test";
import { E2E_BRANCH_ID, E2E_ORG_ID } from "./mock-bound-session";

export const E2E_REGISTER_ID = "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb";
export const E2E_SHIFT_ID = "cccccccc-cccc-cccc-cccc-cccccccccccc";

export type MockShiftState = {
  openShift: boolean;
  denyShifts?: boolean;
  closedShift?: boolean;
  missingRegister?: boolean;
  wrongBranchOnOpen?: boolean;
  /** Reject open when X-Pos-Organization-Id does not match E2E_ORG_ID. */
  wrongOrgOnOpen?: boolean;
  openingCashCountMode?: "Optional" | "Required";
  closingCashCountMode?: "Optional" | "Required";
  denominations?: Array<{ value: number; isEnabled?: boolean }>;
};

function openShiftBody(
  opts: {
    status?: string;
    registerId?: string | null;
  } = {},
) {
  return {
    shiftId: E2E_SHIFT_ID,
    organizationId: E2E_ORG_ID,
    shiftNumber: "S-1001",
    status: opts.status ?? "Open",
    actorId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    registerId: opts.registerId === undefined ? E2E_REGISTER_ID : opts.registerId,
    registerCode: opts.registerId === null ? null : "REG-1",
    registerName: opts.registerId === null ? null : "Front Counter",
    businessDate: "2026-08-21",
    openingCashAmount: 500,
    openingCashCounted: true,
    effectiveCashCountMode: "Required",
    effectiveOpeningCashCountMode: "Required",
    effectiveClosingCashCountMode: "Required",
    openedAtUtc: "2026-08-21T01:00:00Z",
    openedBy: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    createdAtUtc: "2026-08-21T01:00:00Z",
    updatedAtUtc: "2026-08-21T01:00:00Z",
  };
}

/**
 * POS register + cashier-shift API mocks for RMAP-10.
 * Call after mockBoundCashierSession so this handler can fall through / coexist.
 */
export async function mockPosRegisterShiftApi(
  page: Page,
  initial: MockShiftState = { openShift: false },
) {
  let state: MockShiftState = { ...initial };
  let shiftOpeningMode = state.openingCashCountMode ?? "Required";
  let shiftClosingMode = state.closingCashCountMode ?? "Required";
  let denoms =
    state.denominations?.map((d, index) => ({
      denominationId: `dddddddd-dddd-dddd-dddd-dddddddddd${String(index).padStart(2, "0")}`,
      organizationId: E2E_ORG_ID,
      value: d.value,
      displayLabel: null,
      isEnabled: d.isEnabled !== false,
      sortOrder: index,
      updatedAtUtc: "2026-01-01T00:00:00Z",
    })) ??
    [1000, 500, 200, 100, 50, 20, 10, 5, 1, 0.25, 0.1, 0.05].map((value, index) => ({
      denominationId: `dddddddd-dddd-dddd-dddd-dddddddddd${String(index).padStart(2, "0")}`,
      organizationId: E2E_ORG_ID,
      value,
      displayLabel: null,
      isEnabled: true,
      sortOrder: index,
      updatedAtUtc: "2026-01-01T00:00:00Z",
    }));

  function shiftBody(opts: { status?: string; registerId?: string | null } = {}) {
    return {
      ...openShiftBody(opts),
      effectiveCashCountMode: shiftClosingMode,
      effectiveOpeningCashCountMode: shiftOpeningMode,
      effectiveClosingCashCountMode: shiftClosingMode,
    };
  }

  await page.route("**/pos-api/api/v1/pos/**", async (route) => {
    const url = route.request().url();
    const method = route.request().method();
    const headers = route.request().headers();

    if (url.includes("/api/v1/pos/operational-branch") && method === "PUT") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          organizationId: E2E_ORG_ID,
          branchId: E2E_BRANCH_ID,
          name: "Main Branch",
          deviceMatchesSelectedBranch: false,
          deviceBoundBranchId: null,
          openCashierShiftPresent: state.openShift,
        }),
      });
    }

    if (state.denyShifts && url.includes("/cashier-shifts")) {
      return route.fulfill({
        status: 403,
        contentType: "application/json",
        body: JSON.stringify({
          detail: "Shift capability denied",
          errorCode: "capability.denied",
        }),
      });
    }

    if (url.includes("/registers/available-for-shift") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify([
          {
            registerId: E2E_REGISTER_ID,
            registerCode: "REG-1",
            name: "Front Counter",
            status: "Active",
          },
        ]),
      });
    }

    if (
      url.includes("/api/v1/pos/registers") &&
      method === "GET" &&
      !url.includes("available-for-shift")
    ) {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          items: [
            {
              registerId: E2E_REGISTER_ID,
              organizationId: E2E_ORG_ID,
              registerCode: "REG-1",
              name: "Front Counter",
              description: null,
              status: "Active",
              createdAtUtc: "2026-01-01T00:00:00Z",
              createdBy: E2E_ORG_ID,
              updatedAtUtc: "2026-01-01T00:00:00Z",
              updatedBy: E2E_ORG_ID,
              hasOpenShift: state.openShift,
            },
          ],
          totalCount: 1,
          page: 1,
          pageSize: 50,
        }),
      });
    }

    if (url.includes("/operational-setup/cash-denominations") && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(denoms),
      });
    }

    if (url.includes("/operational-setup/cash-denominations") && method === "PUT") {
      const body = route.request().postDataJSON() as {
        items?: Array<{
          value: number;
          isEnabled?: boolean;
          sortOrder?: number;
          denominationId?: string | null;
        }>;
      };
      denoms = (body.items ?? []).map((item, index) => ({
        denominationId:
          item.denominationId ||
          `eeeeeeee-eeee-eeee-eeee-eeeeeeeeee${String(index).padStart(2, "0")}`,
        organizationId: E2E_ORG_ID,
        value: item.value,
        displayLabel: null,
        isEnabled: item.isEnabled !== false,
        sortOrder: item.sortOrder ?? index,
        updatedAtUtc: "2026-01-01T00:00:00Z",
      }));
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(denoms),
      });
    }

    if (
      url.includes("/operational-setup") &&
      method === "GET" &&
      !url.includes("cash-denominations")
    ) {
      const opening = state.openingCashCountMode ?? "Required";
      const closing = state.closingCashCountMode ?? "Required";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          organizationId: E2E_ORG_ID,
          storeDisplayName: "Demo Store",
          currencyCode: "PHP",
          taxPricingMode: "TaxExclusive",
          taxRatePercent: 0,
          receiptHeader: null,
          receiptFooter: null,
          businessAddress: null,
          contactPhone: null,
          defaultRegisterId: E2E_REGISTER_ID,
          cashCountMode: opening,
          openingCashCountMode: opening,
          closingCashCountMode: closing,
          isComplete: true,
          isCompleted: true,
          completedAtUtc: "2026-01-01T00:00:00Z",
          createdAtUtc: "2026-01-01T00:00:00Z",
          createdBy: E2E_ORG_ID,
          updatedAtUtc: "2026-01-01T00:00:00Z",
          updatedBy: E2E_ORG_ID,
        }),
      });
    }

    if (
      url.includes("/operational-setup") &&
      method === "PUT" &&
      !url.includes("cash-denominations")
    ) {
      const body = route.request().postDataJSON() as {
        openingCashCountMode?: string;
        closingCashCountMode?: string;
        cashCountMode?: string;
      };
      state = {
        ...state,
        openingCashCountMode:
          (body.openingCashCountMode as "Optional" | "Required") || state.openingCashCountMode,
        closingCashCountMode:
          (body.closingCashCountMode as "Optional" | "Required") || state.closingCashCountMode,
      };
      const opening = state.openingCashCountMode ?? "Optional";
      const closing = state.closingCashCountMode ?? "Optional";
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          organizationId: E2E_ORG_ID,
          storeDisplayName: "Demo Store",
          currencyCode: "PHP",
          taxPricingMode: "TaxExclusive",
          taxRatePercent: 0,
          cashCountMode: opening,
          openingCashCountMode: opening,
          closingCashCountMode: closing,
          isComplete: true,
          isCompleted: true,
          createdAtUtc: "2026-01-01T00:00:00Z",
          createdBy: E2E_ORG_ID,
          updatedAtUtc: "2026-01-02T00:00:00Z",
          updatedBy: E2E_ORG_ID,
        }),
      });
    }

    if (url.includes("/cashier-shifts/current") && method === "GET") {
      if (!state.openShift) {
        return route.fulfill({ status: 404, contentType: "application/json", body: "{}" });
      }
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(
          shiftBody({
            status: state.closedShift ? "Closed" : "Open",
            registerId: state.missingRegister ? null : E2E_REGISTER_ID,
          }),
        ),
      });
    }

    if (url.includes(`/cashier-shifts/${E2E_SHIFT_ID}/summary`) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify({
          shiftId: E2E_SHIFT_ID,
          shiftNumber: "S-1001",
          status: state.closedShift ? "Closed" : "Open",
          openingCashAmount: 500,
          openingCashCounted: true,
          effectiveCashCountMode: shiftClosingMode,
          netCashSales: 0,
          cashSalesTotal: 0,
          gCashSalesTotal: 0,
          utangSalesTotal: 0,
          cashRefundsTotal: 0,
          totalCashIn: 0,
          totalCashOut: 0,
          expectedCashAmount: 500,
          completedCashCount: 0,
          voidedCashCount: 0,
          completedGCashCount: 0,
          completedUtangCount: 0,
          movements: [],
        }),
      });
    }

    if (url.includes(`/cashier-shifts/${E2E_SHIFT_ID}`) && method === "GET") {
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(
          shiftBody({
            status: state.closedShift ? "Closed" : "Open",
            registerId: state.missingRegister ? null : E2E_REGISTER_ID,
          }),
        ),
      });
    }

    if (url.includes("/cashier-shifts") && method === "POST" && !url.includes("/close")) {
      const branchHeader = headers["x-pos-branch-id"];
      const orgHeader = headers["x-pos-organization-id"];
      if (state.wrongOrgOnOpen || (orgHeader && orgHeader !== E2E_ORG_ID)) {
        return route.fulfill({
          status: 403,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Organization scope mismatch",
            errorCode: "pos.organization.mismatch",
          }),
        });
      }
      if (state.wrongBranchOnOpen || branchHeader !== E2E_BRANCH_ID) {
        return route.fulfill({
          status: 409,
          contentType: "application/json",
          body: JSON.stringify({
            detail: "Operational branch mismatch",
            errorCode: "pos.branch.mismatch",
          }),
        });
      }

      shiftOpeningMode = state.openingCashCountMode ?? "Optional";
      shiftClosingMode = state.closingCashCountMode ?? "Optional";
      state = { ...state, openShift: true, closedShift: false, missingRegister: false };
      return route.fulfill({
        status: 201,
        contentType: "application/json",
        body: JSON.stringify(shiftBody()),
      });
    }

    if (url.includes(`/cashier-shifts/${E2E_SHIFT_ID}/close`) && method === "POST") {
      state = { ...state, openShift: false, closedShift: true };
      return route.fulfill({
        status: 200,
        contentType: "application/json",
        body: JSON.stringify(openShiftBody({ status: "Closed" })),
      });
    }

    // Fall through to other registered handlers (catalog, etc.)
    return route.fallback();
  });

  return {
    setState(next: Partial<MockShiftState>) {
      state = { ...state, ...next };
      if (next.denominations) {
        denoms = next.denominations.map((d, index) => ({
          denominationId: `dddddddd-dddd-dddd-dddd-dddddddddd${String(index).padStart(2, "0")}`,
          organizationId: E2E_ORG_ID,
          value: d.value,
          displayLabel: null,
          isEnabled: d.isEnabled !== false,
          sortOrder: index,
          updatedAtUtc: "2026-01-01T00:00:00Z",
        }));
      }
    },
  };
}
