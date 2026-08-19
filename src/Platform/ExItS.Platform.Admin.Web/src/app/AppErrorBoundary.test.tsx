import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { AppErrorBoundary } from "@/app/AppErrorBoundary";

function BrokenChild(): never {
  throw new Error("forced render failure");
}

describe("AppErrorBoundary", () => {
  it("renders a fallback when a child throws", () => {
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);

    render(
      <AppErrorBoundary>
        <BrokenChild />
      </AppErrorBoundary>,
    );

    expect(screen.getByRole("heading", { name: "Something went wrong" })).toBeInTheDocument();
    consoleError.mockRestore();
  });
});
