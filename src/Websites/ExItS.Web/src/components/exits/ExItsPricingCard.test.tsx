import { render, screen } from "@testing-library/react";

import { ExItsPricingCard } from "./ExItsPricingCard";

describe("ExItsPricingCard", () => {
  it("renders recommended badge and CTA", () => {
    render(
      <ExItsPricingCard
        planName="Growing business"
        price="Pricing TBD"
        features={["Included features per plan TBD"]}
        recommended
        cta={{ href: "/contact", label: "Get Started" }}
      />,
    );

    expect(screen.getByText("Recommended")).toBeVisible();
    expect(screen.getByRole("heading", { name: "Growing business" })).toBeVisible();
    expect(screen.getByText("Pricing TBD")).toBeVisible();
    expect(screen.getByRole("link", { name: /get started/i })).toHaveAttribute(
      "href",
      "/contact",
    );
  });
});
