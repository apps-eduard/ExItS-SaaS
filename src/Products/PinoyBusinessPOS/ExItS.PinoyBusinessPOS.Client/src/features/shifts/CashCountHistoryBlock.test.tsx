import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { CashCountHistoryBlock } from "@/features/shifts/CashCountHistoryBlock";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";

function wrap(ui: ReactNode) {
  return (
    <PreferencesProvider>
      <I18nProvider>{ui}</I18nProvider>
    </PreferencesProvider>
  );
}

describe("CashCountHistoryBlock", () => {
  it("shows amount without toggle when there are no lines", () => {
    render(
      wrap(
        <CashCountHistoryBlock
          label="Opening cash"
          amount={500}
          lines={[]}
          testId="shift-opening-history"
          breakdownLabel="Opening breakdown"
        />,
      ),
    );
    expect(screen.getByTestId("shift-opening-history")).toBeInTheDocument();
    expect(screen.queryByTestId("shift-opening-history-toggle")).not.toBeInTheDocument();
  });

  it("expands denomination breakdown history when lines exist", async () => {
    const user = userEvent.setup();
    render(
      wrap(
        <CashCountHistoryBlock
          label="Counted cash"
          amount={150}
          lines={[
            { denominationValue: 100, quantity: 1, lineTotal: 100 },
            { denominationValue: 50, quantity: 1, lineTotal: 50 },
          ]}
          testId="shift-closing-history"
          breakdownLabel="Closing breakdown"
        />,
      ),
    );
    await user.click(screen.getByTestId("shift-closing-history-toggle"));
    const breakdown = screen.getByTestId("shift-closing-history-breakdown");
    expect(breakdown).toBeInTheDocument();
    expect(breakdown.querySelectorAll("li")).toHaveLength(2);
  });
});
