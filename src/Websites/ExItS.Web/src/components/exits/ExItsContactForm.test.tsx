import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";

import { ExItsContactForm } from "./ExItsContactForm";

describe("ExItsContactForm", () => {
  it("shows accessible validation errors for required general fields", async () => {
    const user = userEvent.setup();
    render(<ExItsContactForm variant="general" />);

    await user.click(screen.getByRole("button", { name: /send message/i }));

    expect(await screen.findByText("Name is required.")).toBeVisible();
    expect(screen.getByText("Enter a valid email address.")).toBeVisible();
    expect(screen.getByText("Message is required.")).toBeVisible();
    expect(screen.getAllByRole("alert")).toHaveLength(3);
  });

  it("does not claim success when the contact endpoint is unavailable", async () => {
    const user = userEvent.setup();
    render(<ExItsContactForm variant="general" />);

    await user.type(screen.getByLabelText(/^name$/i), "Ada Owner");
    await user.type(screen.getByLabelText(/^email$/i), "ada@business.ph");
    await user.type(screen.getByLabelText(/^message$/i), "I want to learn more.");
    await user.click(screen.getByRole("button", { name: /send message/i }));

    expect(await screen.findByRole("status")).toHaveTextContent(/not connected yet/i);
    expect(screen.queryByText(/thank you/i)).not.toBeInTheDocument();
  });
});
