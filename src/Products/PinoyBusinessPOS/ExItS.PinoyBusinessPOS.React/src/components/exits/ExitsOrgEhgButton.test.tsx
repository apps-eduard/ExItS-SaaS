import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import {
  EXITS_ORG_EHG_BTN_DEFAULT_COMMAND,
  ExitsOrgEhgButton,
  exitsOrgEhgButtonClassName,
  exitsOrgEhgButtonCommand,
} from "@/components/exits/ExitsOrgEhgButton";

describe("ExitsOrgEhgButton", () => {
  it("locks solid primary flat md no-shadow by default", () => {
    expect(EXITS_ORG_EHG_BTN_DEFAULT_COMMAND).toBe(
      "ext org-ehg btn-glass solid primary flat md no-shadow",
    );
    expect(exitsOrgEhgButtonCommand()).toBe(EXITS_ORG_EHG_BTN_DEFAULT_COMMAND);
    expect(exitsOrgEhgButtonClassName()).toContain("org-ehg-btn-glass--solid");
    expect(exitsOrgEhgButtonClassName()).toContain("org-ehg-btn-glass--intent-primary");
    expect(exitsOrgEhgButtonClassName()).toContain("org-ehg-btn-glass--appearance-flat");
    expect(exitsOrgEhgButtonClassName()).toContain("org-ehg-btn-glass--size-md");
    expect(exitsOrgEhgButtonClassName()).toContain("org-ehg-btn-glass--no-shadow");
  });

  it("renders recipe data attributes", () => {
    render(<ExitsOrgEhgButton testId="ehg-btn">Save</ExitsOrgEhgButton>);
    const btn = screen.getByTestId("ehg-btn");
    expect(btn).toHaveAttribute("data-recipe", EXITS_ORG_EHG_BTN_DEFAULT_COMMAND);
    expect(btn).toHaveAttribute("data-appearance", "flat");
    expect(btn).toHaveAttribute("data-intent", "primary");
    expect(btn).toHaveClass("org-ehg-btn-glass--solid");
  });
});
