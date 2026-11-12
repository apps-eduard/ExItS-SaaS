import { fireEvent, render, screen, waitFor } from "@testing-library/react";
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
    expect(screen.queryByRole("option", { name: "Choose" })).not.toBeInTheDocument();
    expect(screen.getByRole("option", { name: "Paid" })).toBeInTheDocument();
    const search = screen.getByRole("combobox", { name: "Search Status" });
    await user.type(search, "zz");
    expect(screen.getByText("No matches")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Clear search" }));
    await waitFor(() => expect(search).toHaveFocus());
    expect(search).toHaveValue("");
    expect(screen.getByRole("option", { name: "Paid" })).toBeInTheDocument();
    expect(screen.getByRole("listbox", { name: "Status" })).toBeInTheDocument();
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

  it("updates the value immediately and keeps the closing menu out of the way", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<CategorySelect onChange={onChange} />);
    await user.click(screen.getByRole("combobox", { name: "Category, Groceries" }));
    const menu = document.querySelector("[data-component='select-menu']");
    expect(menu).toHaveAttribute("data-motion", "enter");
    await user.click(screen.getByRole("option", { name: "Rent" }));
    expect(onChange).toHaveBeenCalledWith("rent");
    expect(screen.getByRole("combobox", { name: "Category, Rent" })).toHaveFocus();
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(menu).toHaveAttribute("data-motion", "exit");
    expect(menu).toHaveAttribute("aria-hidden", "true");
    expect(menu).toHaveAttribute("inert");
  });

  it("reopens during the exit and closes from an outside press or a second Escape", async () => {
    const user = userEvent.setup();
    render(<CategorySelect />);
    const trigger = screen.getByRole("combobox", { name: "Category, Groceries" });
    await user.click(trigger);
    await user.keyboard("{Escape}");
    expect(document.querySelector("[data-component='select-menu']")).toHaveAttribute("data-motion", "exit");
    await user.keyboard("{Escape}");
    expect(trigger).toHaveFocus();
    await user.click(trigger);
    expect(screen.getByRole("listbox", { name: "Category" })).toBeInTheDocument();
    fireEvent.pointerDown(document.body);
    expect(screen.queryByRole("listbox")).not.toBeInTheDocument();
    expect(document.querySelector("[data-component='select-menu']")).toHaveAttribute("data-motion", "exit");
  });

  it("removes the menu immediately when motion is reduced", async () => {
    const user = userEvent.setup();
    vi.stubGlobal("matchMedia", (query: string) => ({
      matches: query.includes("prefers-reduced-motion"),
      media: query,
      addEventListener: vi.fn(),
      removeEventListener: vi.fn(),
      addListener: vi.fn(),
      removeListener: vi.fn(),
      dispatchEvent: vi.fn(),
    }));
    render(<CategorySelect />);
    const trigger = screen.getByRole("combobox", { name: "Category, Groceries" });
    await user.click(trigger);
    expect(screen.getByRole("listbox", { name: "Category" })).toBeInTheDocument();
    await user.keyboard("{Escape}");
    expect(document.querySelector("[data-component='select-menu']")).not.toBeInTheDocument();
    expect(trigger).toHaveFocus();
    vi.unstubAllGlobals();
  });

  it("matches outlined and soft fields and keeps stacked above the control", () => {
    const { rerender } = render(
      <Select label="Category" appearance="outlined" defaultValue="rent">
        <option value="rent">Rent</option>
      </Select>,
    );
    const outlined = screen.getByRole("combobox", { name: "Category, Rent" });
    expect(outlined).toHaveAttribute("data-appearance", "outlined");
    expect(outlined.closest("[data-placement='float']")).not.toBeNull();

    rerender(
      <Select label="Category" appearance="soft" defaultValue="rent">
        <option value="rent">Rent</option>
      </Select>,
    );
    const soft = screen.getByRole("combobox", { name: "Category, Rent" });
    expect(soft).toHaveAttribute("data-appearance", "soft");
    expect(soft).toHaveAttribute("data-filled", "true");

    rerender(
      <Select label="Category" appearance="stacked" defaultValue="rent">
        <option value="rent">Rent</option>
      </Select>,
    );
    const stacked = screen.getByRole("combobox", { name: "Category, Rent" });
    expect(stacked).not.toHaveAttribute("data-appearance");
    expect(stacked.closest("[data-placement='float']")).toBeNull();
  });

  it("reuses Input focus motion on the trigger", async () => {
    const user = userEvent.setup();
    render(
      <Select label="Category" appearance="soft" focusMotion="focus-draw" menuMotion="scale" defaultValue="rent">
        <option value="rent">Rent</option>
        <option value="groceries">Groceries</option>
      </Select>,
    );
    const trigger = screen.getByRole("combobox", { name: "Category, Rent" });
    expect(trigger).toHaveAttribute("data-focus-motion", "focus-draw");
    expect(trigger.parentElement).toHaveAttribute("data-motion", "focus-draw");
    trigger.focus();
    expect(trigger.parentElement?.querySelector("[data-focus-draw]")).not.toBeNull();
    await user.click(trigger);
    expect(document.querySelector("[data-component='select-menu']")).toHaveAttribute("data-transition", "scale");
    expect(trigger.parentElement).toHaveAttribute("data-held", "true");
  });

  it("plays the selected menu transition", async () => {
    const user = userEvent.setup();
    const { rerender } = render(
      <Select label="Category" menuMotion="scale" defaultValue="rent">
        <option value="rent">Rent</option>
        <option value="groceries">Groceries</option>
      </Select>,
    );
    await user.click(screen.getByRole("combobox", { name: "Category, Rent" }));
    expect(document.querySelector("[data-component='select-menu']")).toHaveAttribute("data-transition", "scale");
    await user.keyboard("{Escape}");
    rerender(
      <Select label="Category" menuMotion="jump-up" defaultValue="rent">
        <option value="rent">Rent</option>
        <option value="groceries">Groceries</option>
      </Select>,
    );
    await user.click(screen.getByRole("combobox", { name: "Category, Rent" }));
    expect(document.querySelector("[data-component='select-menu']")).toHaveAttribute("data-transition", "jump-up");
    expect(document.querySelector("[data-component='select-menu']")).toHaveAttribute("data-motion", "enter");
  });

  it("filters and keeps a multiple list open", async () => {
    const user = userEvent.setup();
    function Many() {
      const [value, setValue] = useState<string[]>(["groceries"]);
      return (
        <Select
          label="Category"
          multiple
          searchable
          value={value}
          onChange={(event) => setValue([...event.target.selectedOptions].map((option) => option.value))}
        >
          <option value="groceries">Groceries</option>
          <option value="rent">Rent</option>
          <option value="utilities">Utilities</option>
        </Select>
      );
    }
    render(<Many />);
    await user.click(screen.getByRole("button", { name: "Category, Groceries" }));
    expect(screen.getByRole("listbox", { name: "Category" })).toHaveAttribute("aria-multiselectable", "true");
    await user.click(screen.getByRole("option", { name: "Rent" }));
    expect(screen.getByRole("listbox", { name: "Category" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Category, Groceries, Rent" })).toBeInTheDocument();
    const search = screen.getByRole("combobox", { name: "Search Category" });
    await user.type(search, "util");
    await user.keyboard("{Enter}");
    expect(screen.getByRole("button", { name: "Category, 3 selected" })).toBeInTheDocument();
    expect(screen.getByRole("listbox", { name: "Category" })).toBeInTheDocument();
  });

  it("drops a closing menu when the select unmounts", async () => {
    const user = userEvent.setup();
    const errors: unknown[][] = [];
    const spy = vi.spyOn(console, "error").mockImplementation((...args: unknown[]) => {
      errors.push(args);
    });
    const view = render(<CategorySelect />);
    await user.click(screen.getByRole("combobox", { name: "Category, Groceries" }));
    await user.keyboard("{Escape}");
    view.unmount();
    await new Promise((resolve) => window.setTimeout(resolve, 220));
    spy.mockRestore();
    expect(errors).toEqual([]);
    expect(document.querySelector("[data-component='select-menu']")).not.toBeInTheDocument();
  });
});
