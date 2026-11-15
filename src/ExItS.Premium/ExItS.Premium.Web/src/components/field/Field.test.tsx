import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Field } from "./Field";

describe("Field", () => {
  it("associates the label and exposes an error without putting the asterisk in the name", () => {
    render(
      <Field id="name" label="Name" required error="Enter a name.">
        <input id="name" />
      </Field>,
    );
    const field = screen.getByRole("textbox", { name: "Name" });
    expect(field).toBeInTheDocument();
    expect(screen.getByText("Enter a name.")).toHaveAttribute("id", "name-message");
    expect(screen.getByText("*")).toHaveAttribute("aria-hidden", "true");
  });

  it("marks a read-only field in text", () => {
    render(
      <Field id="code" label="Code" readOnly>
        <input id="code" readOnly />
      </Field>,
    );
    expect(screen.getByText("Read-only")).toBeInTheDocument();
    expect(screen.queryByText("*")).not.toBeInTheDocument();
  });
});
