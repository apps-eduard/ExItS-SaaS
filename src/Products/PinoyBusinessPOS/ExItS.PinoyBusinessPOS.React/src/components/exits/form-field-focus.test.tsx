import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { createElement } from "react";
import { Input } from "@/components/ui/input";

const globalsCss = readFileSync(
  resolve(__dirname, "../../styles/globals.css"),
  "utf8",
);
const inputSource = readFileSync(resolve(__dirname, "../ui/input.tsx"), "utf8");

describe("Form Field Focus Standard", () => {
  it("defines thin Primary-aware field focus tokens", () => {
    expect(globalsCss).toMatch(/--exits-field-border-focus:\s*var\(--exits-primary\)/);
    expect(globalsCss).toMatch(/--exits-field-focus-ring-width:\s*1px/);
    expect(globalsCss).toMatch(
      /--exits-field-focus-ring:\s*color-mix\(in srgb, var\(--exits-primary\) 28%, transparent\)/,
    );
    expect(globalsCss).toMatch(/--exits-field-border-error:\s*var\(--exits-danger\)/);
    expect(globalsCss).toMatch(
      /--exits-field-focus-ring-error:\s*color-mix\(in srgb, var\(--exits-danger\) 28%, transparent\)/,
    );
  });

  it("applies thin focus to .exits-input without thick outline/ring-2", () => {
    expect(globalsCss).toMatch(
      /\.exits-input:focus[\s\S]*?box-shadow:\s*0\s+0\s+0\s+var\(--exits-field-focus-ring-width\)/,
    );
    expect(globalsCss).not.toMatch(
      /\.exits-input:focus-visible\s*\{[^}]*outline:\s*2px solid/,
    );
  });

  it("keeps error focus on Danger tokens", () => {
    expect(globalsCss).toMatch(
      /\.exits-input\[aria-invalid="true"\]:focus[\s\S]*?border-color:\s*var\(--exits-field-border-error\)/,
    );
    expect(globalsCss).toMatch(
      /\.exits-input\[aria-invalid="true"\]:focus[\s\S]*?var\(--exits-field-focus-ring-error\)/,
    );
  });

  it("applies thin focus to selects and date inputs", () => {
    expect(globalsCss).toMatch(
      /\.exits-select:focus-visible[\s\S]*?var\(--exits-field-focus-ring-width\)/,
    );
    expect(globalsCss).toMatch(
      /input\[type="date"\]:focus[\s\S]*?var\(--exits-field-focus-ring-width\)/,
    );
    expect(globalsCss).not.toMatch(
      /\.exits-select:focus-visible[\s\S]{0,120}box-shadow:\s*0\s+0\s+0\s+2px\s+var\(--exits-ring\)/,
    );
  });

  it("migrates Search focus to shared field tokens without regressing thin ring", () => {
    expect(globalsCss).toMatch(
      /\.exits-search-field:focus-within[\s\S]*?border-color:\s*var\(--exits-field-border-focus\)/,
    );
    expect(globalsCss).toMatch(
      /\.exits-search-field:focus-within[\s\S]*?box-shadow:\s*0\s+0\s+0\s+var\(--exits-field-focus-ring-width\)/,
    );
  });

  it("uses thin focus utilities on shared Input (no ring-2)", () => {
    expect(inputSource).toMatch(/exits-field-border-focus/);
    expect(inputSource).toMatch(/exits-field-focus-ring-width/);
    expect(inputSource).not.toMatch(/focus-visible:ring-2/);
  });

  it("renders Input with exits-input for CSS inheritance", () => {
    render(createElement(Input, { label: "Product name", name: "productName" }));
    const field = screen.getByLabelText("Product name");
    expect(field.className).toMatch(/exits-input/);
  });

  it("keeps form fields on field radius (Control Shape independence)", () => {
    expect(globalsCss).toMatch(/--exits-field-radius:\s*var\(--exits-radius-md\)/);
  });
});
