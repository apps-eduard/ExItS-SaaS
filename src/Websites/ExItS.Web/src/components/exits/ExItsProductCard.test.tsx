import { render, screen } from "@testing-library/react";

import { ExItsProductCard } from "./ExItsProductCard";

describe("ExItsProductCard", () => {
  it("renders readiness badge and CTA", () => {
    render(
      <ExItsProductCard
        name="Pinoy Business POS"
        description="Flagship POS product."
        badge="available"
        badgeLabel="Available"
        cta={{ label: "Explore", href: "/pos" }}
        featured
      />,
    );

    expect(screen.getByRole("heading", { name: "Pinoy Business POS" })).toBeVisible();
    expect(screen.getByText("Available")).toBeVisible();
    expect(screen.getByRole("link", { name: /explore/i })).toHaveAttribute("href", "/pos");
  });

  it("does not present coming-soon products as available", () => {
    render(
      <ExItsProductCard
        name="Pinoy Service Pro"
        description="Planned service product."
        badge="coming-soon"
        badgeLabel="Coming Soon"
        cta={{ label: "Learn More", href: "/service-pro" }}
      />,
    );

    expect(screen.getByText("Coming Soon")).toBeVisible();
    expect(screen.queryByText("Available")).not.toBeInTheDocument();
  });
});
