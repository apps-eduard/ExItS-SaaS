import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { ExItsNewsletter } from "./ExItsNewsletter";

describe("ExItsNewsletter", () => {
  it("does not claim a successful send when the endpoint is unavailable", async () => {
    const user = userEvent.setup();
    render(<ExItsNewsletter />);

    await user.type(screen.getByLabelText(/email address/i), "owner@business.ph");
    await user.click(screen.getByRole("button", { name: /subscribe/i }));

    expect(
      screen.getByRole("status"),
    ).toHaveTextContent(/not connected yet/i);
  });
});
