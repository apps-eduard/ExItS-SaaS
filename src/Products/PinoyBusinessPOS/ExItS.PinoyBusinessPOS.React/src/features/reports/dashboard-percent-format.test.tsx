import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import {
  DashboardComparisonTrend,
  formatDashboardPercentChange,
} from "@/features/reports/DashboardMetricCards";

describe("formatDashboardPercentChange", () => {
  it("formats ordinary and extreme percentages without changing calculation", () => {
    expect(formatDashboardPercentChange(12.4)).toBe("+12.4%");
    expect(formatDashboardPercentChange(-3)).toBe("-3%");
    expect(formatDashboardPercentChange(0)).toBe("0%");
    expect(formatDashboardPercentChange(5807.4)).toBe("+5807.4%");
  });
});

describe("DashboardComparisonTrend inline styling", () => {
  it("marks positive and negative directions without chip banner classes", () => {
    const { rerender } = render(
      <DashboardComparisonTrend
        comparison={{
          comparisonFromDate: "2026-08-01",
          comparisonToDate: "2026-08-07",
          percentageChange: 12.4,
          percentageAvailable: true,
        }}
        vsPriorLabel="vs previous period"
        variant="inline"
      />,
    );
    expect(screen.getByTestId("dashboard-comparison-trend")).toHaveAttribute(
      "data-trend-direction",
      "up",
    );
    expect(screen.getByTestId("dashboard-comparison-trend").className).toMatch(
      /dashboard-trend--up/,
    );
    expect(screen.getByTestId("dashboard-comparison-trend").className).toMatch(
      /dashboard-trend--inline/,
    );

    rerender(
      <DashboardComparisonTrend
        comparison={{
          comparisonFromDate: "2026-08-01",
          comparisonToDate: "2026-08-07",
          percentageChange: -8.2,
          percentageAvailable: true,
        }}
        vsPriorLabel="vs previous period"
        variant="inline"
      />,
    );
    expect(screen.getByTestId("dashboard-comparison-trend")).toHaveAttribute(
      "data-trend-direction",
      "down",
    );
    expect(screen.getByTestId("dashboard-comparison-trend").className).toMatch(
      /dashboard-trend--down/,
    );
  });
});
