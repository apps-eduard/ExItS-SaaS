import { render, screen } from "@testing-library/react";

import { ExItsButton } from "./ExItsButton";

describe("ExItsButton", () => {
  it("renders provided label", () => {
    render(<ExItsButton>Get Started</ExItsButton>);
    expect(screen.getByRole("button", { name: /get started/i })).toBeVisible();
  });
});

