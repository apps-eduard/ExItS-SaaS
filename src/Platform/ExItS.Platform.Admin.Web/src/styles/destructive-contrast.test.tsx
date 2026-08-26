import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { Button } from "@/components/ui/button";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";

const WCAG_AA_NORMAL_TEXT = 4.5;

function relativeLuminance(hex: string): number {
  const normalized = hex.replace("#", "");
  const channels = [
    parseInt(normalized.slice(0, 2), 16) / 255,
    parseInt(normalized.slice(2, 4), 16) / 255,
    parseInt(normalized.slice(4, 6), 16) / 255,
  ];
  const linear = channels.map((channel) =>
    channel <= 0.03928 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4,
  );
  return 0.2126 * linear[0]! + 0.7152 * linear[1]! + 0.0722 * linear[2]!;
}

function contrastRatio(foreground: string, background: string): number {
  const lighter = Math.max(relativeLuminance(foreground), relativeLuminance(background));
  const darker = Math.min(relativeLuminance(foreground), relativeLuminance(background));
  return (lighter + 0.05) / (darker + 0.05);
}

const lightDestructive = {
  background: "#b42318",
  foreground: "#ffffff",
  hover: "#962018",
};

const darkDestructive = {
  background: "#f08a80",
  foreground: "#1f0a08",
  hover: "#e07a70",
};

/** Mirrors semantic tokens in globals.css for light, dark, and system-dark destructive buttons. */
const themeDestructiveTokens = {
  light: lightDestructive,
  dark: darkDestructive,
  systemLight: lightDestructive,
  systemDark: darkDestructive,
};

describe("destructive button contrast tokens", () => {
  it.each([
    ["light", themeDestructiveTokens.light],
    ["dark", themeDestructiveTokens.dark],
    ["system light", themeDestructiveTokens.systemLight],
    ["system dark", themeDestructiveTokens.systemDark],
  ] as const)("documents %s destructive button contrast at WCAG AA", (_label, tokens) => {
    expect(contrastRatio(tokens.foreground, tokens.background)).toBeGreaterThanOrEqual(WCAG_AA_NORMAL_TEXT);
    expect(contrastRatio(tokens.foreground, tokens.hover)).toBeGreaterThanOrEqual(WCAG_AA_NORMAL_TEXT);
  });

  it("uses destructive foreground token on shared Button variant (Billing Void)", () => {
    render(<Button variant="destructive">Void payment</Button>);
    expect(screen.getByRole("button", { name: "Void payment" })).toHaveClass("text-destructive-foreground");
    expect(screen.getByRole("button", { name: "Void payment" })).not.toHaveClass("text-white");
  });

  it("uses destructive foreground token on destructive confirmation dialogs (Billing Reject)", () => {
    render(
      <ConfirmActionDialog
        open
        title="Reject payment"
        description="Reject this pending manual payment."
        confirmLabel="Reject"
        cancelLabel="Cancel"
        pendingLabel="Submitting…"
        destructive
        onCancel={() => undefined}
        onConfirm={() => undefined}
      />,
    );
    expect(screen.getByRole("button", { name: "Reject" })).toHaveClass("text-destructive-foreground");
  });

  it("keeps disabled destructive buttons on shared disabled tokens", () => {
    render(
      <Button variant="destructive" disabled>
        Void payment
      </Button>,
    );
    const button = screen.getByRole("button", { name: "Void payment" });
    expect(button.className).toContain("disabled:bg-[var(--exits-disabled-bg)]");
    expect(button.className).toContain("disabled:text-[var(--exits-disabled-text)]");
  });
});
