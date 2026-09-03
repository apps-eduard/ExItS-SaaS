import { render, screen } from "@testing-library/react";

import { ExItsBadge } from "./ExItsBadge";

describe("ExItsBadge", () => {
  it("renders readiness badge text", () => {
    render(<ExItsBadge variant="available">Available</ExItsBadge>);
    expect(screen.getByText(/available/i)).toBeVisible();
  });
});

