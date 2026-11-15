import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Card } from "./Card";

describe("Card", () => {
  it("exposes the title as a heading and the elevation level", () => {
    render(
      <Card eyebrow="Today" title="Balance" elevation={2}>
        Ready
      </Card>,
    );
    expect(screen.getByRole("heading", { name: "Balance", level: 3 })).toBeInTheDocument();
    expect(screen.getByRole("article")).toHaveAttribute("data-elevation", "2");
    expect(screen.getByText("Today")).toBeInTheDocument();
  });
});
