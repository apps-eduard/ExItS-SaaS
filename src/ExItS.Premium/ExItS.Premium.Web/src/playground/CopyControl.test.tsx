import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { CopyControl } from "./CopyControl";

describe("CopyControl", () => {
  it("copies the value and announces it", async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    Object.assign(navigator, { clipboard: { writeText } });
    render(<CopyControl label="Neutral 50" value="#f5f6f8" />);
    fireEvent.click(screen.getByRole("button", { name: "Copy Neutral 50" }));
    expect(await screen.findByRole("button", { name: "Copied Neutral 50" })).toBeInTheDocument();
    expect(writeText).toHaveBeenCalledWith("#f5f6f8");
    expect(screen.getByText("Copied #f5f6f8")).toBeInTheDocument();
  });
});
