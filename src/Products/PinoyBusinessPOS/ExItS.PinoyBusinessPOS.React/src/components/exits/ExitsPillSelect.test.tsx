import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";

const SIZES = [
  { value: "XS", label: "XS" },
  { value: "S", label: "S" },
  { value: "M", label: "M" },
  { value: "L", label: "L" },
] as const;

describe("ExitsPillSelect", () => {
  it("selects a single size", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <ExitsPillSelect
        value="M"
        options={[...SIZES]}
        onChange={onChange}
        testId="size"
      />,
    );
    await user.click(screen.getByTestId("size-option-L"));
    expect(onChange).toHaveBeenCalledWith("L");
  });

  it("toggles multi sizes", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <ExitsPillSelect
        mode="multi"
        value={["S", "L"]}
        options={[...SIZES]}
        onChange={onChange}
        testId="sizes"
      />,
    );
    await user.click(screen.getByTestId("sizes-option-S"));
    expect(onChange).toHaveBeenCalledWith(["L"]);
    await user.click(screen.getByTestId("sizes-option-M"));
    expect(onChange).toHaveBeenCalledWith(["S", "L", "M"]);
  });
});
