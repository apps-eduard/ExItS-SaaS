import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { SelectPlayground } from "./SelectPlayground";

describe("SelectPlayground", () => {
  it("updates the live preview from the properties panel", async () => {
    const user = userEvent.setup();
    render(<SelectPlayground />);

    expect(screen.getByRole("heading", { name: "Selects", level: 1 })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Replay open" })).not.toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "States & examples", level: 2 })).toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: "Focus Animation, None" })).toBeInTheDocument();
    expect(screen.getByRole("combobox", { name: "Dropdown Transition, Flip Down" })).toBeInTheDocument();

    await user.click(screen.getByRole("switch", { name: "Searchable" }));
    await user.click(screen.getByRole("switch", { name: "Multiple" }));

    const preview = screen.getAllByRole("button", { name: /Category,/ })[0];
    await user.click(preview);
    expect(screen.getByRole("combobox", { name: "Search Category" })).toBeInTheDocument();
    expect(screen.getByRole("listbox", { name: "Category" })).toHaveAttribute("aria-multiselectable", "true");
    expect(screen.queryByRole("option", { name: "Choose" })).not.toBeInTheDocument();
  });
});
