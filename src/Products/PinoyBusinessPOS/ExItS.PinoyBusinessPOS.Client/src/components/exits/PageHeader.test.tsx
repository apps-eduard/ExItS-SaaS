import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { describe, expect, it } from "vitest";
import { PageHeader } from "@/components/exits/PageHeader";

describe("PageHeader back navigation", () => {
  it("renders no back control when backTo is omitted", () => {
    render(
      <MemoryRouter>
        <PageHeader title="Manager home" description="Operations hub" />
      </MemoryRouter>,
    );
    expect(screen.queryByTestId("page-header-back")).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Manager home" })).toBeInTheDocument();
  });

  it("renders canonical back link with accessible name and 44px touch target", () => {
    render(
      <MemoryRouter>
        <PageHeader
          title="Shifts"
          description="View shifts"
          backTo="/shifts"
          backLabel="Back to shifts"
          backTestId="page-header-back-shifts"
        />
      </MemoryRouter>,
    );
    const back = screen.getByTestId("page-header-back-shifts");
    expect(back).toHaveAttribute("href", "/shifts");
    expect(back).toHaveAccessibleName("Back to shifts");
    expect(back.className).toMatch(/min-h-11/);
    expect(back.className).toMatch(/min-w-11/);
  });
});
