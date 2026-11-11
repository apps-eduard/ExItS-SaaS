import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { AppearanceProvider } from "../../theme/AppearanceProvider";
import { InputMotionGallery } from "./InputMotionGallery";

function preview() {
  return screen.getByRole("region", { name: "Preview" });
}

function locked() {
  return screen.getByRole("region", { name: "Default global" });
}

describe("InputMotionGallery", () => {
  it("uses one preview and keeps the shipped fields in default global", () => {
    render(
      <AppearanceProvider>
        <InputMotionGallery />
      </AppearanceProvider>,
    );

    expect(screen.getByRole("heading", { level: 1, name: "Input playground" })).toBeVisible();
    expect(screen.getByRole("combobox", { name: "Appearance, Stacked" })).toBeVisible();
    expect(screen.getByRole("combobox", { name: "Transition, Border Reveal" })).toBeVisible();
    expect(within(preview()).getByRole("textbox", { name: "Name" })).not.toHaveAttribute("data-appearance");
    expect(screen.getByRole("region", { name: "Appearance comparison" })).toBeVisible();
    expect(screen.queryByRole("heading", { name: "Running Border Accent" })).not.toBeInTheDocument();
    expect(within(preview()).getByRole("textbox", { name: "Name" })).toHaveAttribute("data-focus-motion", "border-reveal");
    expect(within(preview()).getByRole("textbox", { name: "Name" }).parentElement).toHaveAttribute("data-motion", "border-reveal");
    expect(within(locked()).getByRole("textbox", { name: "Name" }).parentElement).not.toHaveAttribute("data-motion");
    expect(within(locked()).getByRole("searchbox", { name: "Search" }).parentElement).not.toHaveAttribute("data-motion");

    expect(within(preview()).getByRole("textbox", { name: "Name" }).parentElement).toHaveAttribute("data-motion", "border-reveal");
    expect(within(locked()).getByRole("textbox", { name: "Name" }).parentElement).not.toHaveAttribute("data-motion");
    expect(screen.getByRole("button", { name: "Replay" })).toBeVisible();
    expect(screen.getAllByRole("textbox", { name: "Name" })).toHaveLength(2);
  });

  it("embeds the playground and applies error only to the preview", async () => {
    const user = userEvent.setup();
    render(
      <AppearanceProvider>
        <InputMotionGallery embedded />
      </AppearanceProvider>,
    );

    expect(screen.getByRole("heading", { name: "Input playground" })).toBeVisible();
    expect(screen.queryByRole("heading", { name: "Input focus motion" })).not.toBeInTheDocument();
    expect(screen.queryByRole("heading", { name: "Shipped focus" })).not.toBeInTheDocument();

    await user.click(screen.getByRole("radio", { name: "Error" }));
    expect(within(preview()).getByText("Use at least two letters.")).toBeVisible();
    expect(within(locked()).queryByText("Use at least two letters.")).not.toBeInTheDocument();

    await user.click(screen.getByRole("combobox", { name: "Appearance, Stacked" }));
    await user.click(screen.getByRole("option", { name: "Outlined" }));
    expect(within(preview()).getByRole("textbox", { name: "Name" })).toHaveAttribute("data-appearance", "outlined");
    expect(within(locked()).getByRole("textbox", { name: "Name" })).not.toHaveAttribute("data-appearance");

    await user.click(screen.getByRole("button", { name: "Default global" }));
    expect(within(preview()).getByRole("textbox", { name: "Name" })).not.toHaveAttribute("data-appearance");

    await user.click(screen.getByRole("radio", { name: "Search" }));
    expect(within(preview()).getByRole("searchbox", { name: "Name" }).parentElement).toHaveAttribute("data-motion", "border-reveal");
    expect(within(locked()).getByRole("searchbox", { name: "Search" })).toBeVisible();
  });
});
