import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { BranchDetailsForm } from "@/features/branches/BranchDetailsForm";

describe("BranchDetailsForm", () => {
  it("shows Philippines defaults as read-only", () => {
    render(
      <BranchDetailsForm
        name="Main Branch"
        contactPhone=""
        addressLine1=""
        addressLine2=""
        city=""
        region=""
        postalCode=""
        branchType="Retail"
        t={(key) => key}
        onChange={() => undefined}
      />,
    );

    expect(screen.getByTestId("branch-timezone")).toHaveAttribute("readonly");
    expect(screen.getByTestId("branch-timezone")).toHaveValue("Asia/Manila");
    expect(screen.getByTestId("branch-country")).toHaveAttribute("readonly");
    expect(screen.getByTestId("branch-country")).toHaveValue("PH");
  });

  it("defaults branch type select to Retail", () => {
    render(
      <BranchDetailsForm
        name="Main Branch"
        contactPhone=""
        addressLine1=""
        addressLine2=""
        city=""
        region=""
        postalCode=""
        branchType="Retail"
        t={(key) => key}
        onChange={() => undefined}
      />,
    );

    expect(screen.getByTestId("branch-type")).toHaveValue("Retail");
  });
});
