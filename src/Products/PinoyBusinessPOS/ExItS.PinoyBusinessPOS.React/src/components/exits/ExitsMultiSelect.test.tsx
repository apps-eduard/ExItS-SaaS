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

  it("supports searchable compact count trigger and hides select-all", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <ExitsMultiSelect
        value={[]}
        searchable
        showSelectAll={false}
        triggerMode="count"
        placeholder="Categories"
        selectedCountLabel={(count) => `Categories · ${count}`}
        options={[
          { value: "a", label: "Alpha", count: 3 },
          { value: "b", label: "Beta", count: 2 },
        ]}
        onChange={onChange}
        testId="cats"
      />,
    );

    expect(screen.getByTestId("cats")).toHaveTextContent("Categories");
    await user.click(screen.getByTestId("cats"));
    expect(screen.getByTestId("cats-search")).toBeInTheDocument();
    expect(screen.queryByTestId("cats-select-all")).not.toBeInTheDocument();
    expect(screen.getByTestId("cats-option-a")).toHaveTextContent("3");
    await user.click(screen.getByTestId("cats-option-a"));
    expect(onChange).toHaveBeenCalledWith(["a"]);
  });
});
