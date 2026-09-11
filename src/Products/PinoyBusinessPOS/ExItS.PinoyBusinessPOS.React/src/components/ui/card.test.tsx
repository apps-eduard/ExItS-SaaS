import { createElement, useState } from "react";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
  Card,
  CardContent,
  CardDescription,
  CardFooter,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import { UI_STANDARDS_DEFAULT_OPEN } from "@/features/ui-standards/ui-standards-disclosure";

describe("ExItS Card visual pilot foundation", () => {
  it("keeps bordered surface defaults compatible with legacy Card usage", () => {
    render(createElement(Card, { "data-testid": "card-basic" }, "Hello"));
    const card = screen.getByTestId("card-basic");
    expect(card.tagName.toLowerCase()).toBe("section");
    expect(card).toHaveAttribute("data-treatment", "bordered");
    expect(card.className).toMatch(/border-border/);
    expect(card.className).toMatch(/bg-surface/);
    expect(card.className).toMatch(/rounded-\[var\(--exits-radius-md\)\]/);
  });

  it("supports treatments including interactive keyboard activation", async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(
      createElement(
        Card,
        {
          treatment: "interactive",
          interactive: true,
          onClick,
          "data-testid": "card-interactive",
          "aria-label": "Open warehouse",
        },
        "Warehouse",
      ),
    );
    const card = screen.getByTestId("card-interactive");
    expect(card.tagName.toLowerCase()).toBe("button");
    expect(card).toHaveAttribute("data-treatment", "interactive");
    await user.click(card);
    expect(onClick).toHaveBeenCalled();
  });

  it("renders selected and accent start warning treatments", () => {
    const { rerender } = render(
      createElement(Card, { selected: true, "data-testid": "card-selected" }, "Selected"),
    );
    expect(screen.getByTestId("card-selected")).toHaveAttribute("data-treatment", "selected");
    expect(screen.getByTestId("card-selected")).toHaveAttribute("data-selected", "true");

    rerender(
      createElement(
        Card,
        {
          treatment: "accent",
          accentTone: "warning",
          accentPosition: "start",
          "data-testid": "card-accent",
        },
        "Warning",
      ),
    );
    expect(screen.getByTestId("card-accent")).toHaveAttribute("data-treatment", "accent");
    expect(screen.getByTestId("card-accent").className).toMatch(/border-s-\[var\(--exits-warning\)\]/);
  });

  it("exposes anatomy helpers", () => {
    render(
      createElement(
        Card,
        { "data-testid": "card-anatomy" },
        createElement(
          CardHeader,
          null,
          createElement(CardTitle, null, "Title"),
          createElement(CardDescription, null, "Description"),
        ),
        createElement(CardContent, null, "Body"),
        createElement(CardFooter, null, "Footer"),
      ),
    );
    expect(screen.getByText("Title")).toBeInTheDocument();
    expect(screen.getByText("Description")).toBeInTheDocument();
    expect(screen.getByText("Body")).toBeInTheDocument();
    expect(screen.getByText("Footer")).toBeInTheDocument();
  });

  it("supports compact padding and soft radius candidates", () => {
    render(
      createElement(
        Card,
        {
          padding: "compact",
          radius: "soft",
          treatment: "elevated",
          "data-testid": "card-compact",
        },
        "Compact",
      ),
    );
    const card = screen.getByTestId("card-compact");
    expect(card.className).toMatch(/rounded-\[var\(--exits-radius-soft\)\]/);
    expect(card.className).toMatch(/px-3/);
    expect(card.className).toMatch(/shadow-\[var\(--exits-shadow-sm\)\]/);
  });

  it("UI Standards cards disclosure defaults match pilot map", () => {
    expect(UI_STANDARDS_DEFAULT_OPEN["cards.treatments"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["cards.kpi"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["cards.entity"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["cards.selectable"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["cards.real-world"]).toBe(true);
    expect(UI_STANDARDS_DEFAULT_OPEN["cards.basic"]).toBe(false);
    expect(UI_STANDARDS_DEFAULT_OPEN["cards.cheatsheet"]).toBe(false);
  });
});

function ControlledSelectable() {
  const [value, setValue] = useState("main");
  return createElement(
    "div",
    {
      role: "radiogroup",
      "aria-label": "Warehouse",
      className: "grid grid-cols-1 gap-2 [grid-template-columns:repeat(auto-fit,minmax(min(100%,13.75rem),1fr))]",
    },
    (["main", "iloilo"] as const).map((key) =>
      createElement(
        Card,
        {
          key,
          as: "button",
          role: "radio",
          "aria-checked": value === key,
          selected: value === key,
          onClick: () => setValue(key),
          "data-testid": `sel-${key}`,
          className: "text-start",
        },
        createElement(
          "div",
          { className: "flex min-w-0 items-start justify-between gap-3" },
          createElement(
            "div",
            { className: "min-w-0 flex-1 text-start" },
            createElement(CardTitle, { as: "h4" }, key === "main" ? "Main Branch" : "Iloilo Warehouse"),
            createElement(CardDescription, null, key === "main" ? "Iloilo" : "Secondary"),
          ),
        ),
      ),
    ),
  );
}

describe("selectable card pattern", () => {
  it("toggles selected state with radio semantics", async () => {
    const user = userEvent.setup();
    render(createElement(ControlledSelectable));
    expect(screen.getByTestId("sel-main")).toHaveAttribute("aria-checked", "true");
    await user.click(screen.getByTestId("sel-iloilo"));
    expect(screen.getByTestId("sel-iloilo")).toHaveAttribute("aria-checked", "true");
    expect(screen.getByTestId("sel-main")).toHaveAttribute("aria-checked", "false");
  });
});
