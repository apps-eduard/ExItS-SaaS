import { useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { Tabs } from "./Tabs";

function Harness() {
  const [value, setValue] = useState<"paid" | "due" | "hold">("paid");
  return (
    <Tabs
      label="Payments"
      value={value}
      onChange={setValue}
      tabs={[
        { value: "paid", label: "Paid", content: <p>Paid panel</p> },
        { value: "due", label: "Due", content: <p>Due panel</p> },
        { value: "hold", label: "Hold", content: <p>Hold panel</p>, disabled: true },
      ]}
    />
  );
}

describe("Tabs", () => {
  it("moves the selection with arrow keys and skips a disabled tab", async () => {
    const user = userEvent.setup();
    render(<Harness />);
    await user.click(screen.getByRole("tab", { name: "Paid" }));
    await user.keyboard("{ArrowRight}");
    expect(screen.getByRole("tab", { name: "Due" })).toHaveAttribute("aria-selected", "true");
    expect(screen.getByText("Due panel")).toBeInTheDocument();
    await user.keyboard("{ArrowRight}");
    expect(screen.getByRole("tab", { name: "Paid" })).toHaveAttribute("aria-selected", "true");
  });
});
