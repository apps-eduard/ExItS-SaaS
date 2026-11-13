import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { Button } from "./Button";

describe("Button", () => {
  it("renders an enabled button", () => {
    render(<Button>Save</Button>);
    expect(screen.getByRole("button", { name: "Save" })).toBeEnabled();
  });

  it("keeps a loading button focusable and ignores activation", async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(
      <Button loading onClick={onClick}>
        Save
      </Button>,
    );
    const button = screen.getByRole("button", { name: "Save" });
    expect(button).toBeEnabled();
    expect(button).toHaveAttribute("aria-busy", "true");
    await user.click(button);
    expect(onClick).not.toHaveBeenCalled();
  });

  it("disables an unavailable action", () => {
    render(<Button disabled>Save</Button>);
    expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
  });

  it("keeps solid as the default appearance", () => {
    render(<Button>Save</Button>);
    expect(screen.getByRole("button", { name: "Save" })).toHaveAttribute("data-appearance", "solid");
  });

  it("exposes soft and outline without changing the tone", () => {
    render(
      <>
        <Button appearance="soft" variant="danger">
          Soft
        </Button>
        <Button appearance="outline" variant="success">
          Outline
        </Button>
        <Button appearance="raise" variant="primary">
          Raise
        </Button>
        <Button appearance="solid-raise" variant="danger">
          Solid raise
        </Button>
      </>,
    );
    expect(screen.getByRole("button", { name: "Soft" })).toHaveAttribute("data-appearance", "soft");
    expect(screen.getByRole("button", { name: "Outline" })).toHaveAttribute("data-appearance", "outline");
    expect(screen.getByRole("button", { name: "Raise" })).toHaveAttribute("data-appearance", "raise");
    expect(screen.getByRole("button", { name: "Solid raise" })).toHaveAttribute("data-appearance", "solid-raise");
  });

  it("covers every semantic role", () => {
    render(
      <>
        <Button variant="accent">Accent</Button>
        <Button variant="info">Info</Button>
        <Button variant="warning">Warning</Button>
        <Button variant="help">Help</Button>
        <Button variant="contrast">Contrast</Button>
      </>,
    );
    expect(screen.getByRole("button", { name: "Accent" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Info" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Warning" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Help" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "Contrast" })).toBeEnabled();
  });
});
