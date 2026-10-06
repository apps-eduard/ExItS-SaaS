import type { ReactElement } from "react";
import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { BranchDetailsForm } from "@/features/branches/BranchDetailsForm";

function renderForm(ui: ReactElement) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>);
}

describe("BranchDetailsForm", () => {
  it("shows Philippines defaults as read-only", () => {
    renderForm(
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
    expect(screen.getByTestId("branch-country")).toBeDisabled();
    expect(screen.getByTestId("branch-country")).toHaveAttribute("data-country-code", "PH");
  });

  it("defaults branch type select to Retail", () => {
    renderForm(
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
