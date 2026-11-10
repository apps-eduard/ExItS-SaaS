import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import iconStyles from "./ExIcon.module.scss?raw";
import { ExIcon } from "./ExIcon";
import { ICON_NAMES } from "./registry";

describe("ExIcon", () => {
  it("hides decorative icons from assistive technology", () => {
    render(<ExIcon name="search" />);
    expect(screen.getByText("search")).toHaveAttribute("aria-hidden", "true");
  });

  it("exposes a label when the icon carries meaning", () => {
    render(<ExIcon name="close" label="Close" />);
    expect(screen.getByRole("img", { name: "Close" })).toBeInTheDocument();
  });

  it("keeps the icon registry unique and the font self-hosted", () => {
    expect(new Set(ICON_NAMES).size).toBe(ICON_NAMES.length);
    expect(iconStyles).toContain("font-display: block");
    expect(iconStyles).toContain("material-symbols-rounded.woff2");
    expect(iconStyles).not.toContain("fonts.googleapis.com");
  });
});
