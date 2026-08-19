import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { App } from "@/app/App";

describe("App foundation", () => {
  it("renders the scaffold through router and QueryClient providers", () => {
    render(<App />);

    expect(screen.getByRole("heading", { name: "ExItS Platform Admin Web" })).toBeInTheDocument();
    expect(screen.getByText(/Scaffold is running/i)).toBeInTheDocument();
  });
});
