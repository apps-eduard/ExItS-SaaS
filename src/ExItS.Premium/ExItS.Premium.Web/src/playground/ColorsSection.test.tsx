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

  it("renders compact semantic, neutral, and palette families", () => {
    render(
      <AppearanceProvider>
        <ColorsSection />
      </AppearanceProvider>,
    );

    expect(screen.getByRole("heading", { name: "Colors", level: 1 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Semantic Colors", level: 2 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Neutral Scale", level: 2 })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Color Palettes", level: 2 })).toBeInTheDocument();

    expect(screen.getByRole("button", { name: /Copy Warning/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Copy neutral-500/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Copy teal-700/i })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: /Copy rose-900/i })).toBeInTheDocument();

    expect(screen.queryByRole("button", { name: /^Copy$/i })).not.toBeInTheDocument();
    expect(screen.queryByText(/Semantic overview/i)).not.toBeInTheDocument();
  });

  it("copies a value when a shade is clicked", async () => {
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
    const copied = copySpy.mock.calls.at(-1)?.[0] ?? "";
    expect(copied.length).toBeGreaterThan(0);
    expect(warning).toHaveAttribute("data-copied", "true");
    expect(within(warning).getByText("Copied")).toBeInTheDocument();
  });
});
