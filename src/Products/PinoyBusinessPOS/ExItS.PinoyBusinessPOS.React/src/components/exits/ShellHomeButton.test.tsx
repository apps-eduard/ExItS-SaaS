import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { ShellHomeButton } from "@/components/exits/ShellHomeButton";

describe("ShellHomeButton", () => {
  it("links to the supplied home href", () => {
    render(
      <MemoryRouter>
        <ShellHomeButton to="/personal" label="Home" testId="shell-home-button" />
      </MemoryRouter>,
    );

    const link = screen.getByTestId("shell-home-button");
    expect(link).toHaveAttribute("href", "/personal");
    expect(link).toHaveAttribute("aria-label", "Home");
  });
});
