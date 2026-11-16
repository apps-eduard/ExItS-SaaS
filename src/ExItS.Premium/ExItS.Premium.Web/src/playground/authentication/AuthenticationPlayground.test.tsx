import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it } from "vitest";
import { AuthenticationPlayground } from "./AuthenticationPlayground";

describe("Authentication playground", () => {
  it("previews each screen and links to the full-screen route", async () => {
    const user = userEvent.setup();
    render(<AuthenticationPlayground />);

    expect(screen.getByRole("heading", { name: "Authentication" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Welcome back" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open full-screen preview" })).toHaveAttribute("href", "/authentication");

    await user.click(screen.getByRole("radio", { name: "Register" }));
    expect(screen.getByRole("heading", { name: "Create your account" })).toBeInTheDocument();
    expect(screen.getByRole("link", { name: "Open full-screen preview" })).toHaveAttribute("href", "/authentication?screen=register");

    await user.click(screen.getByRole("radio", { name: "Forgot" }));
    await user.click(screen.getByRole("button", { name: "Reset instructions" }));
    expect(screen.getByRole("heading", { name: "Reset instructions" })).toBeInTheDocument();

    await user.click(screen.getByRole("radio", { name: "Reset" }));
    await user.click(screen.getByRole("button", { name: "Password updated" }));
    expect(screen.getByRole("heading", { name: "Password updated" })).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: "Authentication failure" }));
    expect(screen.getByRole("alert")).toBeInTheDocument();
  });
});
