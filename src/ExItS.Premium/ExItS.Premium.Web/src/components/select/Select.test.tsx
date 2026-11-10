import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { describe, expect, it, vi } from "vitest";
import { Select } from "./Select";

function CategorySelect({ onChange }: { onChange?: (value: string) => void }) {
  const [value, setValue] = useState("groceries");
  return (
    <Select
      label="Category"
      value={value}
      onChange={(event) => {
        setValue(event.target.value);
        onChange?.(event.target.value);
      }}
    >
      <option value="groceries">Groceries</option>
      <option value="rent">Rent</option>
      <option value="hold" disabled>
        Hold
      </option>
      <option value="utilities">Utilities</option>
    </Select>
  );
}

describe("Select", () => {
  it("replaces the native popup and updates the controlled value", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<CategorySelect onChange={onChange} />);
    const trigger = screen.getByRole("combobox", { name: "Category, Groceries" });
    await user.click(trigger);
    expect(screen.getByRole("listbox", { name: "Category" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Hold" })).toBeDisabled();
    await user.click(screen.getByRole("option", { name: "Rent" }));
    expect(onChange).toHaveBeenCalledWith("rent");
    expect(screen.getByRole("combobox", { name: "Category, Rent" })).toBeInTheDocument();
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
  });

  it("moves with the keyboard, skips disabled options, and closes on Escape", async () => {
    const user = userEvent.setup();
    render(<CategorySelect />);
    const trigger = screen.getByRole("combobox", { name: "Category, Groceries" });
    trigger.focus();
    await user.keyboard("{ArrowDown}{ArrowDown}{ArrowDown}{Enter}");
    expect(screen.getByRole("combobox", { name: "Category, Utilities" })).toHaveFocus();
    await user.keyboard("{ArrowDown}{Escape}");
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
  });

  it("jumps with Home and End and finds an option by typing", async () => {
    const user = userEvent.setup();
    render(<CategorySelect />);
    const trigger = screen.getByRole("combobox", { name: "Category, Groceries" });
    trigger.focus();
    await user.keyboard("{ArrowDown}{End}");
    await user.keyboard("{Enter}");
    expect(screen.getByRole("combobox", { name: "Category, Utilities" })).toBeInTheDocument();
    trigger.focus();
    await user.keyboard("{ArrowDown}r{Enter}");
    expect(screen.getByRole("combobox", { name: "Category, Rent" })).toBeInTheDocument();
  });

  it("keeps one validation message and submits the form value", async () => {
    const user = userEvent.setup();
    let submitted = "";
    render(
      <form
        onSubmit={(event) => {
          event.preventDefault();
          submitted = String(new FormData(event.currentTarget).get("method"));
        }}
      >
        <Select name="method" label="Payment method" defaultValue="cash" error="Choose a method.">
          <option value="">Choose</option>
          <option value="cash">Cash</option>
          <option value="card">Card</option>
        </Select>
        <button type="submit">Save</button>
        <button type="reset">Reset</button>
      </form>,
    );
    const trigger = screen.getByRole("combobox", { name: "Payment method, Cash" });
    expect(trigger).toHaveAttribute("aria-invalid", "true");
    expect(trigger).toHaveAttribute("data-state", "error");
    expect(screen.getByText("Choose a method.")).toBeInTheDocument();
    await user.click(trigger);
    await user.click(screen.getByRole("option", { name: "Card" }));
    await user.click(screen.getByRole("button", { name: "Save" }));
    expect(submitted).toBe("card");
    await user.click(screen.getByRole("button", { name: "Reset" }));
    expect(screen.getByRole("combobox", { name: "Payment method, Cash" })).toBeInTheDocument();
  });

  it("filters a searchable list without changing a closed select", async () => {
    const user = userEvent.setup();
    render(
      <Select label="Status" searchable defaultValue="">
        <option value="">Choose</option>
        <option value="paid">Paid</option>
        <option value="due">Due</option>
      </Select>,
    );
    expect(screen.queryByRole("combobox", { name: "Search Status" })).not.toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Status, Choose" }));
    const search = screen.getByRole("combobox", { name: "Search Status" });
    await user.type(search, "zz");
    expect(screen.getByText("No matches")).toBeInTheDocument();
    await user.clear(search);
    await user.type(search, "due");
    await user.keyboard("{Enter}");
    expect(screen.getByRole("button", { name: "Status, Due" })).toBeInTheDocument();
  });

  it("opens a sheet on a narrow viewport", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("matchMedia", (query: string) => ({
      matches: query.includes("767px"),
      media: query,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));
    render(
      <Select label="Category" defaultValue="rent">
        <option value="rent">Rent</option>
        <option value="groceries">Groceries</option>
      </Select>,
    );
    await user.click(screen.getByRole("combobox", { name: "Category, Rent" }));
    expect(screen.getByRole("dialog", { name: "Category" })).toBeInTheDocument();
    await user.click(screen.getByRole("option", { name: "Groceries" }));
    expect(screen.getByRole("combobox", { name: "Category, Groceries" })).toBeInTheDocument();
    vi.unstubAllGlobals();
  });
});
