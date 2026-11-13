import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Badge, formatBadgeCount } from "./Badge";

describe("Badge", () => {
  it("defaults to soft appearance and neutral tone", () => {
    render(<Badge>Neutral</Badge>);
    const badge = screen.getByText("Neutral").closest("[data-tone]");
    expect(badge).toHaveAttribute("data-tone", "neutral");
    expect(badge).toHaveAttribute("data-appearance", "soft");
    expect(badge).toHaveAttribute("data-all-caps", "false");
    expect(badge).toHaveAttribute("data-indicator", "false");
    expect(badge).toHaveAttribute("data-removable", "false");
  });

  it("applies tone and appearance", () => {
    render(
      <Badge tone="warning" appearance="solid">
        Due
      </Badge>,
    );
    const badge = screen.getByText("Due").closest("[data-tone]");
    expect(badge).toHaveAttribute("data-tone", "warning");
    expect(badge).toHaveAttribute("data-appearance", "solid");
  });

  it("supports emphasis appearance", () => {
    render(
      <Badge tone="success" appearance="emphasis">
        Paid
      </Badge>,
    );
    expect(screen.getByText("Paid").closest("[data-tone]")).toHaveAttribute("data-appearance", "emphasis");
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
    expect(screen.getByText("Secondary").closest("[data-tone]")).toHaveAttribute("data-tone", "secondary");
    expect(screen.getByText("Accent").closest("[data-tone]")).toHaveAttribute("data-tone", "accent");
    expect(screen.getByText("Help").closest("[data-tone]")).toHaveAttribute("data-tone", "help");
    expect(screen.getByText("Contrast").closest("[data-tone]")).toHaveAttribute("data-tone", "contrast");
  });

  it("supports all caps and indicator", () => {
    render(
      <Badge tone="success" allCaps indicator>
        Paid
      </Badge>,
    );
    const badge = screen.getByText("Paid").closest("[data-tone]");
    expect(badge).toHaveAttribute("data-all-caps", "true");
    expect(badge).toHaveAttribute("data-indicator", "true");
    expect(badge?.querySelector("[aria-hidden='true']")).toBeTruthy();
  });

  it("renders indicator-only when there is no label", () => {
    const { container } = render(<Badge tone="danger" indicator aria-label="Failed" />);
    const badge = container.querySelector("[data-indicator-only='true']");
    expect(badge).toHaveAttribute("data-tone", "danger");
    expect(badge).toHaveAttribute("aria-label", "Failed");
  });

  it("calls onRemove from the remove control", async () => {
    const user = userEvent.setup();
    const onRemove = vi.fn();
    render(
      <Badge tone="info" removable onRemove={onRemove}>
        Tag
      </Badge>,
    );
    expect(screen.getByText("Tag").closest("[data-tone]")).toHaveAttribute("data-removable", "true");
    await user.click(screen.getByRole("button", { name: "Remove" }));
    expect(onRemove).toHaveBeenCalledTimes(1);
  });

  it("formats count chips and caps at 99+", () => {
    expect(formatBadgeCount(0)).toBe("0");
    expect(formatBadgeCount(12)).toBe("12");
    expect(formatBadgeCount(99)).toBe("99");
    expect(formatBadgeCount(128)).toBe("99+");

    render(<Badge tone="danger" appearance="solid" count={128} />);
    const badge = screen.getByText("99+").closest("[data-tone]");
    expect(badge).toHaveAttribute("data-count", "true");
    expect(badge).toHaveAttribute("data-removable", "false");
  });
});

