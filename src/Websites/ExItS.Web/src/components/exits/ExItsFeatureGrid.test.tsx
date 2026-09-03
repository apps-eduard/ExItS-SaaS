import { render, screen } from "@testing-library/react";
import { Package } from "lucide-react";

import { ExItsFeatureGrid } from "./ExItsFeatureGrid";

describe("ExItsFeatureGrid", () => {
  it("renders feature titles and bodies", () => {
    render(
      <ExItsFeatureGrid
        columns={2}
        items={[
          {
            title: "Branch stock",
            body: "Each branch manages its own inventory.",
            icon: Package,
          },
        ]}
      />,
    );

    expect(screen.getByRole("heading", { name: "Branch stock" })).toBeVisible();
    expect(screen.getByText(/each branch manages its own inventory/i)).toBeVisible();
  });
});
