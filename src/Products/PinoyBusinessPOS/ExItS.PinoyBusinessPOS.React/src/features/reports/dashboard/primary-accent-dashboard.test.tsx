import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { createElement } from "react";
import { MemoryRouter } from "react-router-dom";
import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { buttonVariants } from "@/components/ui/button";
import { DashboardToolbar } from "@/features/reports/dashboard/DashboardToolbar";
import { ManagerMetricCard } from "@/features/role/ManagerHomeShared";

const globalsCss = readFileSync(
  resolve(__dirname, "../../../styles/globals.css"),
  "utf8",
);

vi.mock("@/i18n/I18nProvider", () => ({
  useI18n: () => ({
    t: (key: string) => key,
  }),
}));

vi.mock("@/features/reports/ReportScopeControls", () => ({
  ReportScopeControls: () => <div data-testid="report-scope-stub" />,
}));

describe("Primary accent + dashboard control shape", () => {
  it("ManagerMetricCard primary tone uses Primary tokens; success stays semantic", () => {
    const { rerender } = render(
      createElement(ManagerMetricCard, {
        label: "Today's sales",
        value: "₱0.00",
        tone: "primary",
        testId: "metric-primary",
      }),
    );
    expect(screen.getByTestId("metric-primary").className).toContain("manager-metric-cell--primary");
    expect(globalsCss).toMatch(
      /\.manager-metric-cell--primary[\s\S]*?color:\s*var\(--exits-primary\)/,
    );
    expect(globalsCss).toMatch(
      /\.manager-metric-cell--success[\s\S]*?color:\s*var\(--exits-success\)/,
    );
    expect(globalsCss).toMatch(
      /\.exits-alert-surface--success|exits-alert-surface--success/,
    );

    rerender(
      createElement(ManagerMetricCard, {
        label: "Shift",
        value: "Open",
        tone: "success",
        testId: "metric-success",
      }),
    );
    expect(screen.getByTestId("metric-success").className).toContain("manager-metric-cell--success");
    expect(screen.getByTestId("metric-success").className).not.toContain(
      "manager-metric-cell--primary",
    );
  });

  it("dashboard hero and KPI emphasis use Primary; success token remains independent", () => {
    expect(globalsCss).toMatch(
      /\.dashboard-hero__value\s*\{[^}]*color:\s*var\(--exits-primary\)/,
    );
    expect(globalsCss).toMatch(
      /\.dashboard-hero\s*\{[^}]*--exits-primary-soft/,
    );
    expect(globalsCss).toMatch(
      /\.dashboard-kpi-chip--emphasis[\s\S]*?--exits-primary/,
    );
    expect(globalsCss).toMatch(
      /\.dashboard-kpi-chip--success[\s\S]*?--exits-success/,
    );
  });

  it("Reports/period/Refresh use control-radius; icon-only auto is square under standard and circle under pill", () => {
    expect(globalsCss).toMatch(
      /\.dashboard-toolbar__reports[\s\S]*?border-radius:\s*var\(--exits-control-radius\)/,
    );
    expect(globalsCss).toMatch(
      /\.dashboard-toolbar__presets[\s\S]*?border-radius:\s*var\(--exits-control-radius\)/,
    );
    expect(globalsCss).toMatch(
      /\.dashboard-toolbar__icon-btn[\s\S]*?border-radius:\s*var\(--exits-control-radius\)/,
    );

    const iconAuto = buttonVariants({ size: "icon", shape: "auto" });
    expect(iconAuto).toContain("rounded-[var(--exits-control-radius)]");
    expect(iconAuto).toContain("size-[var(--exits-control-height)]");
    expect(buttonVariants({ size: "icon", shape: "round" })).toContain("rounded-full");
    expect(buttonVariants({ size: "icon", shape: "round" })).not.toContain(
      "rounded-[var(--exits-control-radius)]",
    );

    expect(globalsCss).toMatch(
      /\[data-control-shape="pill"\][\s\S]*?--exits-control-radius:\s*9999px/,
    );
    expect(globalsCss).toMatch(
      /\[data-control-shape="standard"\][\s\S]*?--exits-control-radius:\s*var\(--exits-radius-md\)/,
    );
  });

  it("DashboardToolbar Refresh is icon-only Button with auto shape and aria-label", () => {
    render(
      createElement(
        MemoryRouter,
        null,
        createElement(DashboardToolbar, {
          preset: "today",
          range: { fromDate: "2026-09-12", toDate: "2026-09-12" },
          custom: { fromDate: "2026-09-12", toDate: "2026-09-12" },
          onPresetChange: () => undefined,
          onCustomChange: () => undefined,
          onApply: () => undefined,
          onRefresh: () => undefined,
          scopeMode: "branch",
          organizationId: "org",
          currentBranchId: "branch",
          currentBranchName: "Main",
          selection: { mode: "current" },
          onSelectionChange: () => undefined,
          allowAllBranches: false,
        }),
      ),
    );

    const refresh = screen.getByTestId("dashboard-refresh");
    expect(refresh.tagName).toBe("BUTTON");
    expect(refresh).toHaveAttribute("aria-label", "dashboard.refresh");
    expect(refresh.className).toContain("rounded-[var(--exits-control-radius)]");
    expect(refresh.className).toContain("size-[var(--exits-control-height)]");
    expect(screen.getByTestId("open-reports-hub").className).toContain("dashboard-toolbar__reports");
  });

  it("keeps form field radius independent of pill control shape", () => {
    expect(globalsCss).toMatch(
      /\[data-control-shape="pill"\][\s\S]*?--exits-field-radius:\s*var\(--exits-radius-md\)/,
    );
    expect(globalsCss).toMatch(
      /\.exits-input\s*\{[^}]*border-radius:\s*var\(--exits-field-radius/,
    );
  });
});
