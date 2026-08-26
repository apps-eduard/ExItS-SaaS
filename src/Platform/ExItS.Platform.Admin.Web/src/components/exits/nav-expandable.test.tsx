import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { NavExpandable } from "@/components/exits/nav-expandable";

describe("NavExpandable", () => {
  it("hides children from the accessibility tree when closed", () => {
    const { rerender } = render(
      <NavExpandable open={false}>
        <a href="/admin">Overview</a>
      </NavExpandable>,
    );
    expect(screen.queryByRole("link", { name: "Overview" })).not.toBeInTheDocument();

    rerender(
      <NavExpandable open>
        <a href="/admin">Overview</a>
      </NavExpandable>,
    );
    expect(screen.getByRole("link", { name: "Overview" })).toBeInTheDocument();
  });
});
