import { render, screen } from "@testing-library/react";

import { ExItsPricingComparisonTable } from "./ExItsPricingComparisonTable";

describe("ExItsPricingComparisonTable", () => {
  it("shows confirmed capability without inventing plan packaging", () => {
    render(
      <ExItsPricingComparisonTable
        caption="Pricing comparison"
        rows={[{ feature: "POS selling", availability: "Confirmed" }]}
      />,
    );

    expect(screen.getByRole("columnheader", { name: /capability/i })).toBeVisible();
    expect(screen.getByText("POS selling")).toBeVisible();
    expect(screen.getByText("Confirmed")).toBeVisible();
    expect(screen.getByText("TBD")).toBeVisible();
  });
});
