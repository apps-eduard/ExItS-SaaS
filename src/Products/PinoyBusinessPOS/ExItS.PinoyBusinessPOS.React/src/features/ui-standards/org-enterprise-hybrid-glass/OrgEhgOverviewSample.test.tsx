import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { OrgEhgOverviewSample } from "@/features/ui-standards/org-enterprise-hybrid-glass/OrgEhgOverviewSample";

describe("OrgEhgOverviewSample", () => {
  it("mounts the Organization Hybrid Glass shell with PHP KPIs", () => {
    render(<OrgEhgOverviewSample />);
    expect(screen.getByTestId("org-ehg-overview")).toHaveAttribute("data-org-ehg");
    expect(screen.getByTestId("org-ehg-shell")).toBeInTheDocument();
    expect(screen.getByTestId("org-ehg-kpis")).toBeInTheDocument();
    expect(screen.getAllByText(/₱|PHP/i).length).toBeGreaterThan(0);
  });
});
