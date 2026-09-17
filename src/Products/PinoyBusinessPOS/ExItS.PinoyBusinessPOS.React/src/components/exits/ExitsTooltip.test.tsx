import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import {
  ExitsTooltip,
  EXITS_TOOLTIP_HOVER_DELAY_MS,
} from "@/components/exits/ExitsTooltip";

describe("ExitsTooltip", () => {
  beforeEach(() => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("portals a professional tooltip after hover delay", async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <ExitsTooltip content="Inventory">
        <button type="button">Inv</button>
      </ExitsTooltip>,
    );

    expect(screen.queryByTestId("exits-tooltip")).not.toBeInTheDocument();
    await user.hover(screen.getByRole("button", { name: "Inv" }));
    vi.advanceTimersByTime(EXITS_TOOLTIP_HOVER_DELAY_MS);
    await waitFor(() => {
      expect(screen.getByTestId("exits-tooltip")).toHaveTextContent("Inventory");
    });
    expect(screen.getByTestId("exits-tooltip").parentElement).toBe(document.body);
  });

  it("does not open when disabled", async () => {
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    render(
      <ExitsTooltip content="Inventory" disabled>
        <button type="button">Inv</button>
      </ExitsTooltip>,
    );
    await user.hover(screen.getByRole("button", { name: "Inv" }));
    vi.advanceTimersByTime(EXITS_TOOLTIP_HOVER_DELAY_MS + 50);
    expect(screen.queryByTestId("exits-tooltip")).not.toBeInTheDocument();
  });
});
