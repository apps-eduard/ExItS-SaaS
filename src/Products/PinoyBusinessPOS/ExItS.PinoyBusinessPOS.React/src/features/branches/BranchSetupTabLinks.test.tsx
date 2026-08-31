import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { BranchSetupTabLinks } from "@/features/branches/BranchSetupTabLinks";

const summary = {
  branchDetailsComplete: true,
  operatingHoursComplete: false,
  deliveryLocationComplete: true,
  deliveryPolicyComplete: false,
  deliveryAreasComplete: true,
  pickupSectionsComplete: 1,
  pickupSectionsTotal: 2,
  deliverySectionsComplete: 2,
  deliverySectionsTotal: 4,
};

describe("BranchSetupTabLinks", () => {
  it("selects overview when activeTab is overview", () => {
    render(
      <MemoryRouter>
        <BranchSetupTabLinks
          branchId="branch-1"
          summary={summary}
          t={(key) => key}
          testIdPrefix="list-branch"
          activeTab="overview"
        />
      </MemoryRouter>,
    );

    expect(screen.getByTestId("list-branch-branch-1-setup-tab-overview")).toHaveAttribute(
      "aria-selected",
      "true",
    );
    expect(screen.getByTestId("list-branch-branch-1-setup-tab-location")).toHaveAttribute(
      "aria-selected",
      "false",
    );
  });

  it("leaves all tabs idle when activeTab is null", () => {
    render(
      <MemoryRouter>
        <BranchSetupTabLinks
          branchId="branch-1"
          summary={summary}
          t={(key) => key}
          testIdPrefix="list-branch"
          activeTab={null}
        />
      </MemoryRouter>,
    );

    expect(screen.getByTestId("list-branch-branch-1-setup-tab-overview")).toHaveAttribute(
      "aria-selected",
      "false",
    );
    expect(screen.getByTestId("list-branch-branch-1-setup-tab-location")).toHaveAttribute(
      "aria-selected",
      "false",
    );
  });

  it("links each setup tab to the branch edit page section", () => {
    render(
      <MemoryRouter>
        <BranchSetupTabLinks
          branchId="branch-1"
          summary={summary}
          t={(key) => key}
          testIdPrefix="list-branch"
        />
      </MemoryRouter>,
    );

    expect(screen.getByTestId("list-branch-branch-1-setup-tab-overview")).toHaveAttribute(
      "href",
      "/org/branches/branch-1/fulfillment",
    );
    expect(screen.getByTestId("list-branch-branch-1-setup-tab-location")).toHaveAttribute(
      "href",
      "/org/branches/branch-1/fulfillment?tab=location",
    );
  });
});
