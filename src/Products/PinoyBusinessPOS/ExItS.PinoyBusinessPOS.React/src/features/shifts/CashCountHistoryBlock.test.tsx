import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { CashCountHistoryBlock } from "@/features/shifts/CashCountHistoryBlock";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
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

describe("CashCountHistoryBlock", () => {
  it("shows Not counted when openingCashCounted=false even if amount is 0", () => {
    render(
      wrap(
        <CashCountHistoryBlock
          label="Opening cash"
          counted={false}
          amount={0}
          lines={[]}
          testId="shift-opening-history"
          breakdownLabel="Opening breakdown"
        />,
      ),
    );
    expect(screen.getByTestId("shift-opening-history-not-counted")).toHaveTextContent(
      "Not counted",
    );
    expect(screen.queryByText(formatPeso(0))).not.toBeInTheDocument();
    expect(screen.queryByTestId("shift-opening-history-toggle")).not.toBeInTheDocument();
  });

  it("shows PHP 0.00 when counted with zero amount", () => {
    render(
      wrap(
        <CashCountHistoryBlock
          label="Opening cash"
          counted={true}
          amount={0}
          lines={[]}
          testId="shift-opening-history"
          breakdownLabel="Opening breakdown"
        />,
      ),
    );
    expect(screen.getByTestId("shift-opening-history-amount")).toHaveTextContent(formatPeso(0));
    expect(screen.queryByTestId("shift-opening-history-not-counted")).not.toBeInTheDocument();
  });

  it("shows manual counted amount without denomination toggle when lines empty", () => {
    render(
      wrap(
        <CashCountHistoryBlock
          label="Opening cash"
          counted={true}
          amount={500}
          lines={[]}
          testId="shift-opening-history"
          breakdownLabel="Opening breakdown"
        />,
      ),
    );
    expect(screen.getByTestId("shift-opening-history-amount")).toHaveTextContent(formatPeso(500));
    expect(screen.queryByTestId("shift-opening-history-toggle")).not.toBeInTheDocument();
  });

  it("expands denomination breakdown including historical 0.01", async () => {
    const user = userEvent.setup();
    render(
      wrap(
        <CashCountHistoryBlock
          label="Closing cash"
          counted={true}
          amount={100.01}
          lines={[
            { denominationValue: 100, quantity: 1, lineTotal: 100 },
            { denominationValue: 0.01, quantity: 1, lineTotal: 0.01 },
          ]}
          testId="shift-closing-history"
          breakdownLabel="Closing breakdown"
        />,
      ),
    );
    await user.click(screen.getByTestId("shift-closing-history-toggle"));
    const breakdown = screen.getByTestId("shift-closing-history-breakdown");
    expect(breakdown.querySelectorAll("li")).toHaveLength(2);
    expect(screen.getByTestId("shift-closing-history-line-0.01")).toBeInTheDocument();
  });

  it("closing skipped shows Not counted and never PHP 0.00", () => {
    render(
      wrap(
        <CashCountHistoryBlock
          label="Closing cash"
          counted={false}
          amount={null}
          lines={[]}
          testId="shift-closing-history"
          breakdownLabel="Closing breakdown"
        />,
      ),
    );
    expect(screen.getByTestId("shift-closing-history-not-counted")).toHaveTextContent(
      "Not counted",
    );
    expect(screen.queryByText(formatPeso(0))).not.toBeInTheDocument();
  });

  it("closing counted zero shows PHP 0.00", () => {
    render(
      wrap(
        <CashCountHistoryBlock
          label="Closing cash"
          counted={true}
          amount={0}
          lines={[]}
          testId="shift-closing-history"
          breakdownLabel="Closing breakdown"
        />,
      ),
    );
    expect(screen.getByTestId("shift-closing-history-amount")).toHaveTextContent(formatPeso(0));
  });
});
