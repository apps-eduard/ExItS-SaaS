import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Badge } from "./Badge";

describe("Badge", () => {
  it("defaults to soft appearance and neutral tone", () => {
    render(<Badge>Neutral</Badge>);
    const badge = screen.getByText("Neutral");
    expect(badge).toHaveAttribute("data-tone", "neutral");
    expect(badge).toHaveAttribute("data-appearance", "soft");
  });

  it("applies tone and appearance", () => {
    render(
      <Badge tone="warning" appearance="solid">
        Due
      </Badge>,
    );
    const badge = screen.getByText("Due");
    expect(badge).toHaveAttribute("data-tone", "warning");
    expect(badge).toHaveAttribute("data-appearance", "solid");
  });

  it("supports emphasis appearance", () => {
    render(
      <Badge tone="success" appearance="emphasis">
        Paid
      </Badge>,
    );
    expect(screen.getByText("Paid")).toHaveAttribute("data-appearance", "emphasis");
  });
});
