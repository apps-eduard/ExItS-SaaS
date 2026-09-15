import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ExitsMultiSelect } from "@/components/exits/ExitsMultiSelect";

describe("ExitsMultiSelect", () => {
  it("toggles options and keeps the menu open", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();

    const { rerender } = render(
      <ExitsMultiSelect
        value={["Cash"]}
        options={[
          { value: "Cash", label: "Cash" },
          { value: "Check", label: "Check" },
        ]}
        onChange={onChange}
        testId="methods"
      />,
    );

    await user.click(screen.getByTestId("methods"));
    await user.click(screen.getByTestId("methods-option-Check"));
    expect(onChange).toHaveBeenCalledWith(["Cash", "Check"]);

    rerender(
      <ExitsMultiSelect
        value={["Cash", "Check"]}
        options={[
          { value: "Cash", label: "Cash" },
          { value: "Check", label: "Check" },
        ]}
        onChange={onChange}
        testId="methods"
      />,
    );
    expect(screen.getByRole("menu")).toBeInTheDocument();
  });
});
