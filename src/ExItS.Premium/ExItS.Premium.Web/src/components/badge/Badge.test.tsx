import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Badge } from "./Badge";

describe("Badge", () => {
  it("defaults to soft appearance and neutral tone", () => {
    render(<Badge>Neutral</Badge>);
    const badge = screen.getByText("Neutral");
    expect(badge).toHaveAttribute("data-tone", "neutral");
    expect(badge).toHaveAttribute("data-appearance", "soft");
    expect(badge).toHaveAttribute("data-all-caps", "false");
    expect(badge).toHaveAttribute("data-indicator", "false");
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

  it("covers the full semantic tone set", () => {
    render(
      <>
        <Badge tone="secondary">Secondary</Badge>
        <Badge tone="accent">Accent</Badge>
        <Badge tone="help">Help</Badge>
        <Badge tone="contrast">Contrast</Badge>
      </>,
    );
    expect(screen.getByText("Secondary")).toHaveAttribute("data-tone", "secondary");
    expect(screen.getByText("Accent")).toHaveAttribute("data-tone", "accent");
    expect(screen.getByText("Help")).toHaveAttribute("data-tone", "help");
    expect(screen.getByText("Contrast")).toHaveAttribute("data-tone", "contrast");
  });

  it("supports all caps and indicator", () => {
    render(
      <Badge tone="success" allCaps indicator>
        Paid
      </Badge>,
    );
    const badge = screen.getByText("Paid");
    expect(badge).toHaveAttribute("data-all-caps", "true");
    expect(badge).toHaveAttribute("data-indicator", "true");
    expect(badge.querySelector("[aria-hidden='true']")).toBeTruthy();
  });

  it("renders indicator-only when there is no label", () => {
    const { container } = render(<Badge tone="danger" indicator aria-label="Failed" />);
    const badge = container.querySelector("[data-indicator-only='true']");
    expect(badge).toHaveAttribute("data-tone", "danger");
    expect(badge).toHaveAttribute("aria-label", "Failed");
  });
});
