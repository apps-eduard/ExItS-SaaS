import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { EmptyState } from "./EmptyState";

describe("EmptyState", () => {
  it("shows the title, description, and action", () => {
    render(<EmptyState title="No records" description="Add a record to start." action={<button type="button">Add record</button>} />);
    expect(screen.getByRole("heading", { name: "No records" })).toBeInTheDocument();
    expect(screen.getByText("Add a record to start.")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Add record" })).toBeInTheDocument();
  });
});
