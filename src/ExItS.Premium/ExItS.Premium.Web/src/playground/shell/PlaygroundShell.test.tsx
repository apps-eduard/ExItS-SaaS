import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { PlaygroundShell } from "./PlaygroundShell";

describe("PlaygroundShell", () => {
  it("renders the preview, properties, and examples layout", () => {
    const { container } = render(
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

    const workspace = container.querySelector("[class*='workspace']");
    const panes = workspace ? [...workspace.children].map((node) => node.getAttribute("aria-label") || node.querySelector("h2")?.textContent) : [];
    expect(panes[0]).toBe("Properties");
    expect(panes[1]).toBe("Preview");
  });

  it("exposes equal panes and two property columns when requested", () => {
    const { container } = render(
      <PlaygroundShell
        title="Button"
        description="Equal layout."
        panes="equal"
        propertiesColumns={2}
        preview={<div>Preview</div>}
        properties={<div>Props</div>}
      />,
    );

    const shell = container.querySelector("section");
    expect(shell).toHaveAttribute("data-panes", "equal");
    expect(shell).toHaveAttribute("data-properties-columns", "2");
  });
});
