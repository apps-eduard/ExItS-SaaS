import { render, screen, within } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { AppearanceProvider } from "../theme/AppearanceProvider";
import { ColorsSection } from "./ColorsSection";
import * as color from "./color";

describe("ColorsSection", () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  it("renders theme tokens and the 25-family reference library", () => {
    render(
      <AppearanceProvider>
        <ColorsSection />
      </AppearanceProvider>,
    );

    expect(screen.getByRole("heading", { name: "Colors", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Semantic Colors", level: 2 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Neutral Scale", level: 2 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Color Palettes", level: 2 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Reference Color Library", level: 2 })).toBeInTheDocument();

    expect(screen.getByRole("button", { name: /Copy Warning/i })).toBeInTheDocument();
    expect(screen.getAllByRole("button", { name: /Copy neutral-500/i }).length).toBeGreaterThanOrEqual(1);
    expect(screen.getAllByRole("button", { name: /Copy teal-700/i }).length).toBeGreaterThanOrEqual(2);
    expect(screen.getByRole("button", { name: /Copy mint-500/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Copy brown-700/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Copy olive-900/i })).toBeInTheDocument();
    expect(screen.getByText(/25 families · 250 swatches/i)).toBeInTheDocument();

    expect(screen.queryByRole("button", { name: /^Copy$/i })).not.toBeInTheDocument();
  });

  it("copies a theme value when a semantic shade is clicked", async () => {
    const copySpy = vi.spyOn(color, "copyText").mockResolvedValue(true);
    const user = userEvent.setup();
    render(
      <AppearanceProvider>
        <ColorsSection />
      </AppearanceProvider>,
    );

    const warning = screen.getByRole("button", { name: /Copy Warning/i });
    await user.click(warning);

    expect(copySpy).toHaveBeenCalled();
    expect(warning).toHaveAttribute("data-copied", "true");
    expect(within(warning).getByText("Copied")).toBeInTheDocument();
  });

  it("searches reference colors and copies HEX or name", async () => {
    const copySpy = vi.spyOn(color, "copyText").mockResolvedValue(true);
    const user = userEvent.setup();
    render(
      <AppearanceProvider>
        <ColorsSection />
      </AppearanceProvider>,
    );

    await user.type(screen.getByLabelText(/Search reference colors/i), "mint-500");
    expect(screen.getByRole("button", { name: /Copy mint-500/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Copy brown-700/i })).not.toBeInTheDocument();

    const mint = screen.getByRole("button", { name: /Copy mint-500/i });
    await user.click(mint);
    expect(copySpy).toHaveBeenLastCalledWith("#2aa978");

    await user.click(screen.getByRole("button", { name: "Name" }));
    await user.click(mint);
    expect(copySpy).toHaveBeenLastCalledWith("mint-500");
  });
});
