import { describe, expect, it, vi } from "vitest";
import { createElement } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Package } from "lucide-react";
import { EmptyState } from "@/components/exits/EmptyState";

describe("EmptyState", () => {
  it("renders title, detail, and decorative icon", () => {
    render(
      createElement(EmptyState, {
        title: "No customers yet",
        detail: "Add your first customer when you are ready.",
        icon: createElement(Package, { "data-testid": "empty-icon" }),
        align: "center",
      }),
    );
    expect(screen.getByTestId("exits-empty-state")).toHaveAttribute("data-align", "center");
    expect(screen.getByTestId("exits-empty-state")).toHaveAttribute("data-variant", "default");
    expect(screen.getByText("No customers yet")).toBeInTheDocument();
    expect(screen.getByText("Add your first customer when you are ready.")).toBeInTheDocument();
    expect(screen.getByTestId("empty-icon").closest("[aria-hidden]")).toBeTruthy();
  });

  it("omits detail and actions when not provided", () => {
    render(createElement(EmptyState, { title: "No recent activity", size: "compact" }));
    expect(screen.getByTestId("exits-empty-state")).toHaveAttribute("data-size", "compact");
    expect(screen.queryByText("Add")).not.toBeInTheDocument();
  });

  it("renders primary and secondary actions", async () => {
    const user = userEvent.setup();
    const onPrimary = vi.fn();
    const onSecondary = vi.fn();
    render(
      createElement(EmptyState, {
        title: "No matching customers",
        detail: "Try changing your search or filters.",
        variant: "filtered",
        action: createElement("button", { type: "button", onClick: onPrimary }, "Clear filters"),
        secondaryAction: createElement(
          "button",
          { type: "button", onClick: onSecondary },
          "Reset",
        ),
      }),
    );
    expect(screen.getByTestId("exits-empty-state")).toHaveAttribute("data-variant", "filtered");
    await user.click(screen.getByRole("button", { name: "Clear filters" }));
    await user.click(screen.getByRole("button", { name: "Reset" }));
    expect(onPrimary).toHaveBeenCalledTimes(1);
    expect(onSecondary).toHaveBeenCalledTimes(1);
  });
});
