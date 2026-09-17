import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ExitsSelect } from "@/components/exits/ExitsSelect";

describe("ExitsSelect", () => {
  it("opens a themed listbox and selects an option", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    render(
      <ExitsSelect
        value="Cash"
        options={[
          { value: "Cash", label: "Cash" },
          { value: "ManualGCash", label: "Manual GCash" },
          { value: "Check", label: "Check" },
        ]}
        onChange={onChange}
        testId="payment-method"
      />,
    );

    await user.click(screen.getByTestId("payment-method"));
    expect(screen.getByRole("menu")).toBeInTheDocument();
    await user.click(screen.getByTestId("payment-method-option-ManualGCash"));
    expect(onChange).toHaveBeenCalledWith("ManualGCash");
  });
});
