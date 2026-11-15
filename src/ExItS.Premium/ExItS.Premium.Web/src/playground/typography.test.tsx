import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import globalCss from "../styles/global.scss?raw";
import tokens from "../styles/tokens.scss?raw";
import playgroundCss from "./Playground.module.scss?raw";
import { Foundations } from "./Foundations";

const ROLES = ["display", "page", "section", "card", "body", "secondary", "label", "caption", "numeric"] as const;

describe("typography playground", () => {
  it("shows the nine locked roles, density, wrapping, and truncation", () => {
    render(<Foundations id="typography" />);

    for (const name of ["Display", "Page title", "Section heading", "Card title", "Body", "Supporting text", "Label", "Caption", "Numeric"]) {
      expect(screen.getAllByText(name).length).toBeGreaterThan(0);
    }
    expect(screen.getAllByText("Body stays 16px.")).toHaveLength(3);
    expect(screen.getByRole("region", { name: "Compact type" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Standard type" })).toBeInTheDocument();
    expect(screen.getByRole("region", { name: "Large type" })).toBeInTheDocument();
    expect(screen.getByText("North market groceries and household account")).toHaveStyle({ textOverflow: "ellipsis" });

    expect([...document.querySelectorAll("[data-role]")].map((node) => node.getAttribute("data-role"))).toEqual(
      ROLES.map((role) => `type-${role}`),
    );
  });

  it("binds each role class to the locked tokens and keeps body at 1rem", () => {
    for (const role of ROLES) {
      expect(globalCss).toContain(`.type-${role} {`);
      expect(globalCss).toContain(`font-size: var(--type-${role}-size);`);
      expect(globalCss).toContain(`font-weight: var(--type-${role}-weight);`);
      expect(globalCss).toContain(`line-height: var(--type-${role}-line);`);
      expect(tokens).toContain(`--type-${role}-size:`);
      expect(tokens).toContain(`--type-${role}-weight:`);
      expect(tokens).toContain(`--type-${role}-line:`);
    }
    expect(tokens).toContain("--type-body-size: 1rem;");
    expect(tokens).not.toContain("--type-body-size: var(--density");
    expect(tokens).toContain("--type-numeric-size: clamp(1.625rem, 1.3rem + 0.7vw, 2rem);");
    expect(tokens).toContain("--type-numeric-weight: 600;");
    expect(tokens).toContain("--type-numeric-line: 1.1;");
    expect(playgroundCss).toContain("text-overflow: ellipsis;");
    expect(playgroundCss).toContain("font-size: clamp(1.75rem, 1.4rem + 0.8vw, 2.25rem);");
    expect(playgroundCss).toContain("font-size: clamp(1.625rem, 1.5rem + 0.25vw, 1.75rem);");
    expect(playgroundCss).not.toContain("--type-numeric-balanced");
    expect(playgroundCss).not.toContain("--type-numeric-minimal");
  });

  it("locks balanced numeric as production and keeps the other two as labels", () => {
    render(<Foundations id="typography" />);
    expect(screen.getByRole("region", { name: "Financial numeric" })).toBeInTheDocument();
    expect(screen.getByText("B · Balanced")).toBeInTheDocument();
    expect(screen.getByText("A · Previous")).toBeInTheDocument();
    expect(screen.getByText("C · Minimal")).toBeInTheDocument();

    const primary = [...document.querySelectorAll('[data-amount="primary"]')];
    expect(primary.map((node) => node.getAttribute("data-numeric"))).toEqual(["balanced", "previous", "minimal"]);
    expect(primary[0]?.className).toContain("type-numeric");
    expect(primary[1]?.className).not.toContain("type-numeric");
    expect(primary[2]?.className).not.toContain("type-numeric");
    for (const node of document.querySelectorAll('[data-amount="long"]')) {
      const style = getComputedStyle(node);
      expect(style.textOverflow).not.toBe("ellipsis");
      expect(style.whiteSpace).not.toBe("nowrap");
      expect(style.overflowWrap).toBe("break-word");
    }
  });
});
