import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import {
  EXITS_EXT_BTN_DEFAULT_COMMAND,
  ExitsExtButton,
  exitsExtButtonCommand,
} from "@/components/exits/ExitsExtButton";

describe("ExitsExtButton", () => {
  it("locks secondary glossy-raise ripple by default", () => {
    expect(EXITS_EXT_BTN_DEFAULT_COMMAND).toBe("ext btn secondary glossy-raise ripple");
    expect(exitsExtButtonCommand()).toBe(EXITS_EXT_BTN_DEFAULT_COMMAND);
  });

  it("renders recipe data attributes", () => {
    render(<ExitsExtButton testId="ext-btn">Save</ExitsExtButton>);
    const btn = screen.getByTestId("ext-btn");
    expect(btn).toHaveAttribute("data-recipe", EXITS_EXT_BTN_DEFAULT_COMMAND);
    expect(btn).toHaveAttribute("data-appearance", "glossy-raise");
    expect(btn).toHaveAttribute("data-intent", "secondary");
    expect(btn).toHaveAttribute("data-ripple", "ripple");
    expect(btn).toHaveClass("exits-ext-btn--glossy-raise");
  });
});
