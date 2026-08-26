import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import type { PosCashierShiftDto, PosCashierShiftSummaryDto } from "@/api/pos/pos-shifts-client";
import { ShiftCashHistoryPanel } from "@/features/shifts/ShiftCashHistoryPanel";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { formatPeso } from "@/lib/format-money";

function wrap(ui: ReactNode) {
  return (
    <PreferencesProvider>
      <I18nProvider>{ui}</I18nProvider>
    </PreferencesProvider>
  );
}

function baseShift(overrides: Partial<PosCashierShiftDto> = {}): PosCashierShiftDto {
  return {
    shiftId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    organizationId: "11111111-1111-1111-1111-111111111111",
    shiftNumber: "S-1001",
    status: "Closed",
    actorId: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    registerId: "bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb",
    registerCode: "REG-1",
    registerName: "Front",
    businessDate: "2026-08-21",
    openingCashAmount: 500,
    openingCashCounted: true,
    effectiveCashCountMode: "Optional",
    effectiveOpeningCashCountMode: "Optional",
    effectiveClosingCashCountMode: "Optional",
    openedAtUtc: "2026-08-21T01:00:00Z",
    openedBy: "dddddddd-dddd-dddd-dddd-dddddddddddd",
    createdAtUtc: "2026-08-21T01:00:00Z",
    updatedAtUtc: "2026-08-21T05:00:00Z",
    ...overrides,
  };
}

function baseSummary(
  overrides: Partial<PosCashierShiftSummaryDto> = {},
): PosCashierShiftSummaryDto {
  return {
    shiftId: "cccccccc-cccc-cccc-cccc-cccccccccccc",
    shiftNumber: "S-1001",
    status: "Closed",
    openingCashAmount: 500,
    openingCashCounted: true,
    effectiveCashCountMode: "Optional",
    netCashSales: 1200,
    cashSalesTotal: 1200,
    gCashSalesTotal: 50,
    utangSalesTotal: 25,
    cashRefundsTotal: 100,
    totalCashIn: 40,
    totalCashOut: 20,
    expectedCashAmount: 1620,
    closingCashAmount: 1620,
    cashVarianceAmount: 0,
    closingCashCountState: "Counted",
    completedCashCount: 1,
    voidedCashCount: 0,
    completedGCashCount: 0,
    completedUtangCount: 0,
    ...overrides,
  };
}

describe("ShiftCashHistoryPanel", () => {
  it("7+9 opening/closing policy from effective modes; legacy fallback", () => {
    const { rerender } = render(
      wrap(
        <ShiftCashHistoryPanel
          closed
          shift={baseShift({
            effectiveOpeningCashCountMode: "Required",
            effectiveClosingCashCountMode: "Optional",
            closingCashAmount: 100,
            closingCashCountState: "Counted",
          })}
          summary={baseSummary()}
        />,
      ),
    );
    expect(screen.getByTestId("shift-opening-policy-value")).toHaveTextContent("Required");
    expect(screen.getByTestId("shift-closing-policy-value")).toHaveTextContent("Optional");

    rerender(
      wrap(
        <ShiftCashHistoryPanel
          closed
          shift={baseShift({
            effectiveOpeningCashCountMode: null,
            effectiveClosingCashCountMode: null,
            effectiveCashCountMode: "Required",
            closingCashAmount: 100,
            closingCashCountState: "Counted",
          })}
          summary={baseSummary()}
        />,
      ),
    );
    expect(screen.getByTestId("shift-opening-policy-value")).toHaveTextContent("Required");
    expect(screen.getByTestId("shift-closing-policy-value")).toHaveTextContent("Required");
  });

  it("15+16 uses server summary drawer totals and does not bake GCash/Utang into expected", () => {
    render(
      wrap(
        <ShiftCashHistoryPanel
          closed
          shift={baseShift({
            closingCashAmount: 1620,
            closingCashCountState: "Counted",
            cashVarianceAmount: 0,
          })}
          summary={baseSummary({
            cashSalesTotal: 1200,
            cashRefundsTotal: 100,
            totalCashIn: 40,
            totalCashOut: 20,
            expectedCashAmount: 1620,
            gCashSalesTotal: 50,
            utangSalesTotal: 25,
            cashVarianceAmount: 0,
          })}
        />,
      ),
    );
    expect(screen.getByTestId("shift-cash-sales")).toHaveTextContent(formatPeso(1200));
    expect(screen.getByTestId("shift-cash-refunds")).toHaveTextContent(formatPeso(100));
    expect(screen.getByTestId("shift-cash-in")).toHaveTextContent(formatPeso(40));
    expect(screen.getByTestId("shift-cash-out")).toHaveTextContent(formatPeso(20));
    expect(screen.getByTestId("shift-expected-cash")).toHaveTextContent(formatPeso(1620));
    expect(screen.getByTestId("shift-expected-cash")).not.toHaveTextContent(formatPeso(1695));
    expect(screen.getByTestId("shift-gcash-sales")).toHaveTextContent(formatPeso(50));
    expect(screen.getByTestId("shift-utang-sales")).toHaveTextContent(formatPeso(25));
  });

  it("12+13+14 Balanced / Over / Short", () => {
    const { rerender } = render(
      wrap(
        <ShiftCashHistoryPanel
          closed
          shift={baseShift({ closingCashAmount: 1620, closingCashCountState: "Counted" })}
          summary={baseSummary({ cashVarianceAmount: 0 })}
        />,
      ),
    );
    expect(screen.getByTestId("shift-variance-balanced")).toHaveTextContent("Balanced");

    rerender(
      wrap(
        <ShiftCashHistoryPanel
          closed
          shift={baseShift({ closingCashAmount: 1650, closingCashCountState: "Counted" })}
          summary={baseSummary({ cashVarianceAmount: 30 })}
        />,
      ),
    );
    expect(screen.getByTestId("shift-variance-over")).toHaveTextContent("Over by");

    rerender(
      wrap(
        <ShiftCashHistoryPanel
          closed
          shift={baseShift({ closingCashAmount: 1600, closingCashCountState: "Counted" })}
          summary={baseSummary({ cashVarianceAmount: -20 })}
        />,
      ),
    );
    expect(screen.getByTestId("shift-variance-short")).toHaveTextContent("Short by");
  });

  it("5+6 closing skipped vs counted zero", () => {
    const { rerender } = render(
      wrap(
        <ShiftCashHistoryPanel
          closed
          shift={baseShift({
            closingCashAmount: null,
            closingCashCountState: "NotPerformed",
          })}
          summary={baseSummary({ closingCashAmount: null, cashVarianceAmount: null })}
        />,
      ),
    );
    expect(screen.getByTestId("shift-closing-history-not-counted")).toHaveTextContent(
      "Not counted",
    );

    rerender(
      wrap(
        <ShiftCashHistoryPanel
          closed
          shift={baseShift({
            closingCashAmount: 0,
            closingCashCountState: "Counted",
          })}
          summary={baseSummary({ closingCashAmount: 0, cashVarianceAmount: 0 })}
        />,
      ),
    );
    expect(screen.getByTestId("shift-closing-history-amount")).toHaveTextContent(formatPeso(0));
  });

  it("10+11 historical lines including 0.01 are shown; current config never injected", async () => {
    const user = userEvent.setup();
    render(
      wrap(
        <ShiftCashHistoryPanel
          closed
          shift={baseShift({
            openingCashAmount: 100.01,
            openingDenominationLines: [
              { denominationValue: 100, quantity: 1, lineTotal: 100 },
              { denominationValue: 0.01, quantity: 1, lineTotal: 0.01 },
            ],
            closingCashAmount: 100.01,
            closingCashCountState: "Counted",
            closingDenominationLines: [{ denominationValue: 0.01, quantity: 1, lineTotal: 0.01 }],
          })}
          summary={baseSummary({
            expectedCashAmount: 100.01,
            closingCashAmount: 100.01,
            cashVarianceAmount: 0,
            gCashSalesTotal: 0,
            utangSalesTotal: 0,
          })}
        />,
      ),
    );
    await user.click(screen.getByTestId("shift-opening-history-toggle"));
    expect(screen.getByTestId("shift-opening-history-line-0.01")).toBeInTheDocument();
    expect(screen.queryByText("999")).not.toBeInTheDocument();
    expect(screen.queryByText("CURRENT-ONLY")).not.toBeInTheDocument();
  });
});
