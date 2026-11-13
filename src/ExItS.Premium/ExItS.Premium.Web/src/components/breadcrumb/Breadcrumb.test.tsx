import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Breadcrumb } from "./Breadcrumb";

describe("Breadcrumb", () => {
  it("renders an accessible trail with the current page marked", () => {
    render(
      <Breadcrumb
        items={[
          { id: "design-system", label: "Design System", href: "#overview", icon: "dashboard" },
          { id: "branch-Buttons", label: "Buttons", href: "#button" },
          { id: "button", label: "Button" },
        ]}
      />,
    );

    const nav = screen.getByRole("navigation", { name: "Breadcrumb" });
    expect(nav).toBeInTheDocument();
    expect(screen.getByRole("link", { name: /Design System/i })).toHaveAttribute("href", "#overview");
    expect(screen.getByRole("link", { name: "Buttons" })).toHaveAttribute("href", "#button");
    expect(screen.getByText("Button").closest("[aria-current='page']")).toBeInTheDocument();
    expect(screen.queryByRole("link", { name: "Button" })).not.toBeInTheDocument();
  });

  it("truncates long labels", () => {
    render(
      <Breadcrumb
        maxLabelLength={12}
        items={[
          { id: "home", label: "Design System", href: "#overview" },
          { id: "long", label: "Very Long Section Name" },
        ]}
      />,
    );

    expect(screen.getByText("Very Long S…")).toBeInTheDocument();
  });

  it("calls onNavigate when a prior crumb is activated", async () => {
    const user = userEvent.setup();
    const onNavigate = vi.fn();
    render(
      <Breadcrumb
        onNavigate={onNavigate}
        items={[
          { id: "design-system", label: "Design System", href: "#overview" },
          { id: "button", label: "Button" },
        ]}
      />,
    );

    await user.click(screen.getByRole("link", { name: "Design System" }));
    expect(onNavigate).toHaveBeenCalledWith(
      expect.objectContaining({ id: "design-system", href: "#overview" }),
    );
  });

  it("collapses middle crumbs on narrow viewports via data attributes", () => {
    const { container } = render(
      <Breadcrumb
        items={[
          { id: "design-system", label: "Design System", href: "#overview" },
          { id: "branch-Buttons", label: "Buttons", href: "#button" },
          { id: "button", label: "Button" },
        ]}
      />,
    );

    expect(container.querySelector('[data-middle="true"]')).toBeInTheDocument();
    expect(container.querySelector('[class*="collapse"]')).toBeInTheDocument();
  });
});
