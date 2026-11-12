import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PlaygroundShell } from "./PlaygroundShell";

describe("PlaygroundShell", () => {
  it("renders the preview, properties, and examples layout", () => {
    render(
      <PlaygroundShell
        title="Selects"
        description="Live field with properties."
        preview={<div>Live preview</div>}
        properties={<div>Property controls</div>}
        examples={<div>State gallery</div>}
      />,
    );

    expect(screen.getByRole("heading", { name: "Selects", level: 1 })).toBeInTheDocument();
    expect(screen.getByText("Live field with properties.")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Preview", level: 2 })).toBeInTheDocument();
    expect(screen.getByText("Live preview")).toBeInTheDocument();
    expect(screen.getByRole("complementary", { name: "Properties" })).toBeInTheDocument();
    expect(screen.getByText("Property controls")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "States & examples", level: 2 })).toBeInTheDocument();
    expect(screen.getByText("State gallery")).toBeInTheDocument();
  });
});
