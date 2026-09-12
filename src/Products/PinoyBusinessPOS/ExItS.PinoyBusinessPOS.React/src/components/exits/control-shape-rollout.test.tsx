import { createElement } from "react";
import { MemoryRouter } from "react-router-dom";
import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ActionChipBar } from "@/components/exits/ActionChipBar";
import { actionChipItemVariants } from "@/components/exits/action-chip-variants";
import { CountBadge } from "@/components/exits/CountChip";
import { ExitsChipBar } from "@/components/exits/ExitsChipBar";
import { FilterChip } from "@/components/exits/FilterChip";
import { filterChipVariants } from "@/components/exits/chip-variants";
import { SearchField } from "@/components/exits/SearchField";
import { StatusChip } from "@/components/exits/StatusChip";
import { TagChip } from "@/components/exits/TagChip";
import { Button, buttonVariants } from "@/components/ui/button";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const globalsCss = readFileSync(
  resolve(__dirname, "../../styles/globals.css"),
  "utf8",
);

describe("POS global Control Shape rollout", () => {
  it("Button auto follows control-radius; explicit shapes win; round stays round", () => {
    expect(buttonVariants({ shape: "auto" })).toContain("rounded-[var(--exits-control-radius)]");
    expect(buttonVariants({ shape: "standard" })).toContain("rounded-[var(--exits-radius-md)]");
    expect(buttonVariants({ shape: "standard" })).not.toContain(
      "rounded-[var(--exits-control-radius)]",
    );
    expect(buttonVariants({ shape: "pill" })).toContain("rounded-full");
    expect(buttonVariants({ shape: "round", size: "icon" })).toContain("rounded-full");

    render(<Button type="button">Save</Button>);
    expect(screen.getByRole("button", { name: "Save" }).className).toContain(
      "rounded-[var(--exits-control-radius)]",
    );

    render(
      <Button type="button" shape="round" size="icon" aria-label="Edit">
        *
      </Button>,
    );
    expect(screen.getByRole("button", { name: "Edit" }).className).toContain("rounded-full");
  });

  it("Action Chip and Filter Chip default to auto control-radius", () => {
    expect(actionChipItemVariants({ shape: "auto" })).toContain(
      "rounded-[var(--exits-control-radius)]",
    );
    expect(filterChipVariants({ shape: "auto" })).toContain(
      "rounded-[var(--exits-control-radius)]",
    );
    expect(filterChipVariants({ shape: "pill" })).toContain("rounded-full");
    expect(filterChipVariants({ shape: "pill" })).not.toContain(
      "rounded-[var(--exits-control-radius)]",
    );

    render(createElement(FilterChip, { children: "Active" }));
    expect(screen.getByRole("button", { name: "Active" })).toHaveAttribute("data-shape", "auto");
    expect(screen.getByRole("button", { name: "Active" }).className).toContain(
      "rounded-[var(--exits-control-radius)]",
    );

    render(
      createElement(
        MemoryRouter,
        null,
        createElement(ActionChipBar, {
          ariaLabel: "Actions",
          items: [{ key: "a", label: "Stock Count" }],
        }),
      ),
    );
    expect(screen.getByRole("button", { name: "Stock Count" }).className).toContain(
      "rounded-[var(--exits-control-radius)]",
    );
  });

  it("Search Field defaults to auto and CSS ties search radius to control-radius", () => {
    render(
      createElement(SearchField, {
        label: "Search products",
        placeholder: "Search products...",
        value: "",
        onChange: () => undefined,
      }),
    );
    const shell = screen.getByTestId("exits-search-field");
    expect(shell).toHaveAttribute("data-shape", "auto");
    expect(globalsCss).toMatch(/--exits-search-radius:\s*var\(--exits-control-radius\)/);
  });

  it("ExitsChipBar filter/actions use control-radius; steps keep fixed md radius", () => {
    expect(globalsCss).toMatch(
      /\.exits-chip\s*\{[^}]*border-radius:\s*var\(--exits-control-radius\)/,
    );
    expect(globalsCss).toMatch(
      /\.exits-chip-bar--steps\s+\.exits-chip\s*\{[^}]*border-radius:\s*var\(--exits-radius-md\)/,
    );
    expect(globalsCss).toMatch(
      /\.exits-filter-pill\s*\{[^}]*border-radius:\s*var\(--exits-control-radius\)/,
    );

    render(
      createElement(ExitsChipBar, {
        ariaLabel: "Scope",
        variant: "filter",
        items: [{ key: "all", label: "All", state: "active", onSelect: () => undefined }],
      }),
    );
    expect(screen.getByRole("tab", { name: "All" }).className).toContain("exits-chip");
  });

  it("form fields stay on field-radius under pill preference tokens", () => {
    expect(globalsCss).toMatch(
      /\[data-control-shape="pill"\][\s\S]*?--exits-control-radius:\s*9999px/,
    );
    expect(globalsCss).toMatch(
      /\[data-control-shape="pill"\][\s\S]*?--exits-field-radius:\s*var\(--exits-radius-md\)/,
    );
    expect(globalsCss).toMatch(
      /\.exits-input\s*\{[^}]*border-radius:\s*var\(--exits-field-radius/,
    );
    expect(globalsCss).toMatch(
      /select\.exits-input[\s\S]*?border-radius:\s*var\(--exits-field-radius/,
    );
    // Textarea inherits .exits-input field radius (only overrides min-height/resize).
    expect(globalsCss).toContain("textarea.exits-input");
    expect(globalsCss).not.toMatch(
      /textarea\.exits-input\s*\{[^}]*border-radius:\s*9999px/,
    );
  });

  it("StatusChip, TagChip, CountBadge, and round icon buttons keep explicit shapes", () => {
    render(createElement(StatusChip, { tone: "success", children: "Active" }));
    expect(screen.getByText("Active")).toHaveAttribute("data-shape", "pill");
    expect(screen.getByText("Active").className).not.toContain(
      "rounded-[var(--exits-control-radius)]",
    );

    render(createElement(TagChip, { tone: "info", children: "Beta" }));
    expect(screen.getByText("Beta").closest("[data-shape]")).toHaveAttribute("data-shape", "square");
    expect(screen.getByText("Beta").closest("[data-shape]")?.className).not.toContain(
      "rounded-[var(--exits-control-radius)]",
    );

    render(createElement(CountBadge, { tone: "primary", count: "3" }));
    expect(screen.getByText("3").className).toContain("rounded-full");

    expect(buttonVariants({ shape: "round", size: "icon" })).toContain("rounded-full");
    expect(buttonVariants({ shape: "round", size: "icon" })).not.toContain(
      "rounded-[var(--exits-control-radius)]",
    );
  });
});
