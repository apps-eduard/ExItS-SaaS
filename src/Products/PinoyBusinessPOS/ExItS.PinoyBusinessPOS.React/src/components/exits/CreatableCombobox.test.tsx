import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { CreatableCombobox } from "@/components/exits/CreatableCombobox";
import { useState } from "react";

function Harness({ initial = "" }: { initial?: string }) {
  const [value, setValue] = useState(initial);
  return (
    <CreatableCombobox
      value={value}
      onChange={setValue}
      options={["Purchasing", "Sales", "Operations"]}
      testId="dept"
      createLabel={(q) => `Add “${q}”`}
    />
  );
}

describe("CreatableCombobox", () => {
  it("filters defaults and creates a custom value", async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(screen.getByTestId("dept-trigger"));
    await user.type(screen.getByTestId("dept-search"), "Purch");
    expect(screen.getByText("Purchasing")).toBeInTheDocument();
    expect(screen.queryByText("Sales")).not.toBeInTheDocument();

    await user.clear(screen.getByTestId("dept-search"));
    await user.type(screen.getByTestId("dept-search"), "Head Barista");
    await user.click(screen.getByTestId("dept-create"));
    expect(screen.getByTestId("dept-trigger")).toHaveTextContent("Head Barista");
  });
});
