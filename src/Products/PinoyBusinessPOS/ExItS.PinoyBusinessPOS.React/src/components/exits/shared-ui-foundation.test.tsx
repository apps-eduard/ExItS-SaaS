import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { SearchField } from "@/components/exits/SearchField";
import { FilterButton, FilterChips, ListToolbar, SortButton } from "@/components/exits/ListToolbar";
import { MoneyDisplay, QuantityStepper } from "@/components/exits/MoneyQuantity";
import { ConfirmationDialog } from "@/components/exits/SheetDialog";

describe("shared UI foundation", () => {
  it("SearchField renders accessible label and clear action", async () => {
    const user = userEvent.setup();
    const onClear = vi.fn();
    render(
      <SearchField
        label="Search products"
        value="rice"
        onChange={() => undefined}
        onClear={onClear}
      />,
    );
    expect(screen.getByLabelText("Search products")).toHaveValue("rice");
    await user.click(screen.getByLabelText("Clear search"));
    expect(onClear).toHaveBeenCalledOnce();
  });

  it("ListToolbar composes filters and chips", () => {
    const onRemove = vi.fn();
    render(
      <ListToolbar
        search={<SearchField label="Search" value="" onChange={() => undefined} />}
        filters={<FilterButton activeCount={2}>Filters</FilterButton>}
        sort={<SortButton>Sort</SortButton>}
        chips={<FilterChips items={[{ id: "active", label: "Active" }]} onRemove={onRemove} />}
      />,
    );
    expect(screen.getByTestId("list-toolbar")).toBeInTheDocument();
    expect(screen.getByText("2")).toBeInTheDocument();
    expect(screen.getByLabelText("Remove filter Active")).toHaveClass("exits-filter-pill");
    expect(screen.getByRole("button", { name: /Filters/i })).toHaveClass("exits-filter-pill");
  });

  it("MoneyDisplay and QuantityStepper are touch-friendly", async () => {
    const user = userEvent.setup();
    const onIncrement = vi.fn();
    const onDecrement = vi.fn();
    render(
      <>
        <MoneyDisplay amount={1250.5} testId="money" />
        <QuantityStepper
          value={2}
          increaseLabel="Increase"
          decreaseLabel="Decrease"
          onIncrement={onIncrement}
          onDecrement={onDecrement}
        />
      </>,
    );
    expect(screen.getByTestId("money").textContent).toMatch(/1,250\.50|1250\.50/);
    await user.click(screen.getByLabelText("Increase"));
    await user.click(screen.getByLabelText("Decrease"));
    expect(onIncrement).toHaveBeenCalledOnce();
    expect(onDecrement).toHaveBeenCalledOnce();
  });

  it("ConfirmationDialog supports keyboard-visible actions", async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    const onCancel = vi.fn();
    render(
      <ConfirmationDialog
        open
        title="Confirm"
        detail="Are you sure?"
        confirmLabel="Yes"
        cancelLabel="No"
        onConfirm={onConfirm}
        onCancel={onCancel}
      />,
    );
    await user.click(screen.getByRole("button", { name: "Yes" }));
    expect(onConfirm).toHaveBeenCalledOnce();
  });
});
