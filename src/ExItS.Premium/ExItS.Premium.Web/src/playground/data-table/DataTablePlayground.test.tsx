import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { DataTablePlayground } from "./DataTablePlayground";
import { DataTablePreviewPage } from "./DataTablePreviewPage";

describe("Data Table screen preview", () => {
  it("defaults to a 1280px iframe and does not render the ledger in the parent page", () => {
    render(<DataTablePlayground />);

    expect(screen.getByRole("combobox", { name: "Screen Preview, Desktop — 1280px" })).toBeInTheDocument();
    expect(screen.getByText("Viewport width 1280px")).toBeInTheDocument();
    const frame = screen.getByTitle("Data Table preview at 1280px");
    expect(frame).toHaveAttribute("src", "/data-table-preview");
    expect(frame).toHaveStyle({ width: "1280px", height: "800px" });
    expect(screen.queryByRole("heading", { name: "Transactions" })).not.toBeInTheDocument();
  });

  it("changes the iframe viewport width without nesting another playground", async () => {
    const user = userEvent.setup();
    render(<DataTablePlayground />);

    await user.click(screen.getByRole("combobox", { name: "Screen Preview, Desktop — 1280px" }));
    await user.click(screen.getByRole("option", { name: "Mobile — 390px" }));

    const frame = screen.getByTitle("Data Table preview at 390px");
    expect(frame).toHaveAttribute("src", "/data-table-preview");
    expect(frame).toHaveStyle({ width: "390px" });
    expect(frame).toHaveAttribute("data-viewport-width", "390");
    expect(document.querySelectorAll("iframe")).toHaveLength(1);
  });

  it("renders the ledger on the preview route without another iframe", () => {
    render(<DataTablePreviewPage />);

    expect(screen.getByRole("heading", { name: "Transactions" })).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: "Export" }).length).toBeGreaterThan(0);
    expect(screen.getByRole("radio", { name: "Compact" })).toBeInTheDocument();
    expect(screen.queryByRole("combobox", { name: /Screen Preview/ })).not.toBeInTheDocument();
    expect(document.querySelector("iframe")).toBeNull();
  });
});
