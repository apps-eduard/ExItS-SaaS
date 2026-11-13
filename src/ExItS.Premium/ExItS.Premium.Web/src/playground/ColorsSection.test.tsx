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

  it(
    "renders semantic, foundation, and the 40-family reference library",
    () => {
      const { container } = render(
        <AppearanceProvider>
          <ColorsSection />
        </AppearanceProvider>,
      );

      expect(screen.getByRole("heading", { name: "Colors", level: 1 })).toBeInTheDocument();
      expect(screen.getByRole("heading", { name: "Semantic Colors", level: 2 })).toBeInTheDocument();
      expect(screen.getByRole("heading", { name: "Foundation Colors", level: 2 })).toBeInTheDocument();
      expect(screen.getByRole("heading", { name: "Neutral Scale", level: 2 })).toBeInTheDocument();
      expect(screen.getByRole("heading", { name: "Color Palettes", level: 2 })).toBeInTheDocument();
      expect(screen.getByRole("heading", { name: "Reference Color Library", level: 2 })).toBeInTheDocument();

      expect(screen.getByRole("button", { name: /Copy Warning/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Copy Success/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Copy Info/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Copy Help/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Copy Secondary/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Copy Accent/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Copy Contrast/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Copy Page/i })).toBeInTheDocument();
      expect(screen.getByRole("button", { name: /Copy Overlay/i })).toBeInTheDocument();

      expect(container.querySelectorAll("[data-reference-family]")).toHaveLength(40);
      expect(container.querySelectorAll("[data-reference-family] [data-hex]")).toHaveLength(400);
      expect(screen.getByText(/40 families · 400 swatches/i)).toBeInTheDocument();
    },
    15000,
  );

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

    await user.type(screen.getByLabelText(/Search reference colors/i), "sage-500");
    expect(screen.getByRole("button", { name: /Copy sage-500/i })).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /Copy magenta-900/i })).not.toBeInTheDocument();

    const sage = screen.getByRole("button", { name: /Copy sage-500/i });
    await user.click(sage);
    expect(copySpy).toHaveBeenLastCalledWith("#628068");

    await user.click(screen.getByRole("button", { name: "Name" }));
    await user.click(sage);
    expect(copySpy).toHaveBeenLastCalledWith("sage-500");
  });
});
