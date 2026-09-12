import { describe, expect, it } from "vitest";
import { createElement } from "react";
import { render, screen } from "@testing-library/react";
import { Notice } from "@/components/exits/Notice";

describe("Notice", () => {
  it.each([
    ["info", "status", "polite"],
    ["warning", "status", "polite"],
    ["success", "status", "polite"],
    ["danger", "alert", "assertive"],
  ] as const)("tone %s uses role=%s live=%s", (tone, role, live) => {
    render(
      createElement(Notice, {
        tone,
        testId: `notice-${tone}`,
        children: `${tone} message`,
      }),
    );
    const el = screen.getByTestId(`notice-${tone}`);
    expect(el).toHaveAttribute("role", role);
    expect(el).toHaveAttribute("aria-live", live);
    expect(el).toHaveAttribute("data-tone", tone);
    expect(el).toHaveTextContent(`${tone} message`);
  });

  it("supports title and action", () => {
    render(
      createElement(Notice, {
        tone: "warning",
        title: "Attention",
        action: createElement("button", { type: "button" }, "Review"),
        children: "Some products are low in stock.",
      }),
    );
    expect(screen.getByText("Attention")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Review" })).toBeInTheDocument();
  });

  it("hides icon when icon={null}", () => {
    const { container } = render(
      createElement(Notice, {
        tone: "info",
        icon: null,
        children: "No icon",
      }),
    );
    expect(container.querySelector("[aria-hidden]")).toBeNull();
  });
});
