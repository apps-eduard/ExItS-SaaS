import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { ExItsWaitlistForm } from "./ExItsWaitlistForm";

describe("ExItsWaitlistForm", () => {
  it("does not claim a successful waitlist signup when the endpoint is unavailable", async () => {
    const user = userEvent.setup();
    render(<ExItsWaitlistForm />);

    await user.type(screen.getByLabelText(/email address/i), "owner@business.ph");
    await user.click(screen.getByRole("button", { name: /get notified/i }));

    expect(screen.getByRole("status")).toHaveTextContent(/not connected yet/i);
  });
});
