import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { QuantityStepper } from "@/components/exits/MoneyQuantity";

describe("QuantityStepper editable mode", () => {
  it("increments and decrements with plus/minus", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={2}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );

    await user.click(screen.getByLabelText("Increase"));
    expect(onChange).toHaveBeenLastCalledWith(3);
    rerender(
      <QuantityStepper
        value={3}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).toHaveBeenLastCalledWith(2);
  });

  it("disables minus at minimum and never emits zero", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByLabelText("Decrease")).toBeDisabled();
    await user.click(screen.getByLabelText("Decrease"));
    expect(onChange).not.toHaveBeenCalled();
  });

  it("allows direct typing for whole units", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={1}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "4");
    expect(onChange).toHaveBeenCalledWith(4);
  });

  it("supports weighted decimal quantities and rejects excess precision", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(
      <QuantityStepper
        value={0.5}
        onChange={onChange}
        min={0.001}
        step={0.001}
        precision={3}
        unit="Kg"
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByText("Kg")).toBeInTheDocument();
    const input = screen.getByTestId("qty");
    await user.clear(input);
    await user.type(input, "1.25");
    expect(onChange).toHaveBeenCalledWith(1.25);

    onChange.mockClear();
    await user.clear(input);
    await user.type(input, "1.2345");
    expect(onChange).not.toHaveBeenCalledWith(1.2345);
  });

  it("supports ArrowUp/ArrowDown and disabled state", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(
      <QuantityStepper
        value={2}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    const input = screen.getByTestId("qty");
    await user.click(input);
    await user.keyboard("{ArrowUp}");
    expect(onChange).toHaveBeenLastCalledWith(3);
    await user.keyboard("{ArrowDown}");
    expect(onChange).toHaveBeenLastCalledWith(1);

    rerender(
      <QuantityStepper
        value={2}
        onChange={onChange}
        min={1}
        step={1}
        precision={0}
        disabled
        decreaseLabel="Decrease"
        increaseLabel="Increase"
        ariaLabel="Qty"
        valueTestId="qty"
      />,
    );
    expect(screen.getByLabelText("Increase")).toBeDisabled();
    expect(screen.getByLabelText("Decrease")).toBeDisabled();
    expect(screen.getByTestId("qty")).toBeDisabled();
  });

  it("keeps legacy callback mode for display-only value", async () => {
    const user = userEvent.setup();
    const onIncrement = vi.fn();
    const onDecrement = vi.fn();
    render(
      <QuantityStepper
        value={2}
        increaseLabel="Increase"
        decreaseLabel="Decrease"
        onIncrement={onIncrement}
        onDecrement={onDecrement}
      />,
    );
    expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
    await user.click(screen.getByLabelText("Increase"));
    await user.click(screen.getByLabelText("Decrease"));
    expect(onIncrement).toHaveBeenCalledOnce();
    expect(onDecrement).toHaveBeenCalledOnce();
  });
});
