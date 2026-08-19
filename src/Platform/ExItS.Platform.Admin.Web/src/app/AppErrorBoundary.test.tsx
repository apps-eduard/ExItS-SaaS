import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { AppErrorBoundary } from "@/app/AppErrorBoundary";
import { PreferencesProvider } from "@/hooks/use-preferences";

function BrokenChild() {
  throw new Error("forced render failure");
  return null;
}

describe("AppErrorBoundary", () => {
  it("renders a fallback when a child throws", () => {
    const consoleError = vi.spyOn(console, "error").mockImplementation(() => undefined);

    render(
      <PreferencesProvider>
        <AppErrorBoundary>
          <BrokenChild />
        </AppErrorBoundary>
      </PreferencesProvider>,
    );

    expect(screen.getByRole("heading", { name: "Something went wrong" })).toBeInTheDocument();
    consoleError.mockRestore();
  });
});
