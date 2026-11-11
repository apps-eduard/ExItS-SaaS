import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { SearchField } from "../search/SearchField";
import { Input } from "./Input";

describe("Input", () => {
  it("keeps the shipped focus treatment unless a variant is requested", () => {
    render(<Input label="Name" placeholder="Account name" />);
    const field = screen.getByRole("textbox", { name: "Name" });
    expect(field).toHaveAttribute("data-focus-motion", "none");
    expect(field.parentElement).not.toHaveAttribute("data-motion");
    expect(screen.queryByText("Required")).not.toBeInTheDocument();
    expect(screen.queryByText("Read-only")).not.toBeInTheDocument();
  });

  it("opts into a focus variant without changing the value", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<Input label="Name" focusMotion="border-reveal" onChange={onChange} />);
    const field = screen.getByRole("textbox", { name: "Name" });
    expect(field.parentElement).toHaveAttribute("data-motion", "border-reveal");
    await user.type(field, "Jo");
    expect(field).toHaveValue("Jo");
    expect(field).toHaveAttribute("data-filled", "true");
    expect(field).toHaveAttribute("data-focus-motion", "border-reveal");
    expect(onChange).toHaveBeenCalledTimes(2);
  });

  it("shows error and success as text", () => {
    render(
      <>
        <Input label="Email" defaultValue="a" error="Use at least two letters." />
        <Input label="Store" defaultValue="Market" success="Name is available." />
      </>,
    );
    const email = screen.getByRole("textbox", { name: "Email" });
    expect(email).toHaveAttribute("aria-invalid", "true");
    expect(email).toHaveAccessibleDescription("Use at least two letters.");
    expect(screen.getByText("Use at least two letters.")).toBeVisible();
    expect(screen.getByRole("textbox", { name: "Store" })).toHaveAccessibleDescription("Name is available.");
  });

  it("skips focus motion when the field is disabled", () => {
    render(<Input label="Locked" value="Locked" disabled focusMotion="running-accent" onChange={() => undefined} />);
    const field = screen.getByRole("textbox", { name: "Locked" });
    expect(field).toBeDisabled();
    expect(field).toHaveAttribute("data-focus-motion", "running-accent");
    expect(field.parentElement).not.toHaveAttribute("data-motion");
  });

  it("marks read-only and required without relying on color", () => {
    render(
      <>
        <Input label="Nickname" value="Published" readOnly focusMotion="soft-glow" onChange={() => undefined} />
        <Input label="Code" required focusMotion="soft-glow" />
      </>,
    );
    const nickname = screen.getByRole("textbox", { name: "Nickname, read-only" });
    expect(nickname).toHaveAttribute("readonly");
    expect(nickname.parentElement).toHaveAttribute("data-motion", "soft-glow");
    expect(nickname.parentElement).toHaveAttribute("data-quiet", "true");
    expect(screen.getByText("Read-only")).toBeVisible();
    const code = screen.getByRole("textbox", { name: "Code" });
    expect(code).toBeRequired();
    expect(screen.getByText("*")).toBeVisible();
    expect(screen.queryByText("Required")).not.toBeInTheDocument();
  });
});

describe("SearchField focus motion", () => {
  it("keeps clear and the shipped shell unless a variant is requested", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    const { rerender } = render(<SearchField label="Search" value="Market" onChange={onChange} />);
    const shipped = screen.getByRole("searchbox", { name: "Search" });
    expect(shipped).toHaveAttribute("data-focus-motion", "none");
    expect(shipped.parentElement).not.toHaveAttribute("data-motion");
    await user.click(screen.getByRole("button", { name: "Clear search" }));
    expect(onChange).toHaveBeenCalledWith("");

    rerender(<SearchField label="Search" value="Market" onChange={onChange} focusMotion="running-accent" />);
    const accent = screen.getByRole("searchbox", { name: "Search" });
    expect(accent.parentElement).toHaveAttribute("data-motion", "running-accent");
    expect(screen.getByRole("button", { name: "Clear search" })).toBeEnabled();
  });
});
