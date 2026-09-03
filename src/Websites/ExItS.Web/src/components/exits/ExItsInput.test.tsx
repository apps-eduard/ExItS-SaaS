import { render, screen } from "@testing-library/react";

import { ExItsInput } from "./ExItsInput";

describe("ExItsInput", () => {
  it("renders input with placeholder", () => {
    render(
      <ExItsInput placeholder="Email address" aria-label="Email address" />,
    );
    expect(
      screen.getByPlaceholderText(/email address/i),
    ).toBeVisible();
  });
});

