import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { AppearanceProvider } from "../../theme/AppearanceProvider";
import { InputMotionGallery } from "./InputMotionGallery";

describe("InputMotionGallery", () => {
  it("shows the current treatment beside the three experimental variants", () => {
    render(
      <AppearanceProvider>
        <InputMotionGallery />
      </AppearanceProvider>,
    );

    expect(screen.getByRole("heading", { name: "Input focus motion" })).toBeVisible();
    expect(screen.getByRole("heading", { name: "Current" })).toBeVisible();
    expect(screen.getByRole("heading", { name: "Animated Border Reveal" })).toBeVisible();
    expect(screen.getByRole("heading", { name: "Soft Focus Glow" })).toBeVisible();
    expect(screen.getByRole("heading", { name: "Running Border Accent" })).toBeVisible();
    expect(screen.getByRole("button", { name: "Replay accent" })).toBeVisible();
    expect(screen.getByRole("textbox", { name: "Current name" }).parentElement).not.toHaveAttribute("data-motion");
    expect(screen.getByRole("textbox", { name: "Animated Border Reveal name" }).parentElement).toHaveAttribute("data-motion", "border-reveal");
    expect(screen.getByRole("textbox", { name: "Soft Focus Glow name" }).parentElement).toHaveAttribute("data-motion", "soft-glow");
    expect(screen.getByRole("textbox", { name: "Running Border Accent name" }).parentElement).toHaveAttribute("data-motion", "running-accent");
    expect(screen.getByRole("textbox", { name: "Animated Border Reveal disabled" })).toBeDisabled();
    expect(screen.getAllByText("Use at least two letters.")).toHaveLength(4);
  });
});
