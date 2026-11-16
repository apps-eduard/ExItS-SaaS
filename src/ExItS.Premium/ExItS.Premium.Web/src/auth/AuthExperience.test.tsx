import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AuthExperience } from "./AuthExperience";

function renderAuth(props: Partial<Parameters<typeof AuthExperience>[0]> = {}) {
  return render(<AuthExperience frame="preview" submitDelayMs={0} {...props} />);
}

function inputByLabel(name: RegExp) {
  const match = screen.getAllByLabelText(name).find((element) => element instanceof HTMLInputElement);
  if (!match) throw new Error(`No input labelled ${name}`);
  return match;
}

describe("authentication screens", () => {
  it("validates login, toggles the password, and does not store the attempt", async () => {
    const user = userEvent.setup();
    const setItem = vi.spyOn(Storage.prototype, "setItem");
    renderAuth();

    await user.click(screen.getByRole("button", { name: "Sign in" }));
    expect(screen.getByText("Enter an email address.")).toBeInTheDocument();
    expect(screen.getByText("Enter your password.")).toBeInTheDocument();

    await user.type(inputByLabel(/^email/i), "ada@example.com");
    await user.type(inputByLabel(/^password/i), "ledger-1947");
    expect(inputByLabel(/^password/i)).toHaveAttribute("type", "password");
    await user.click(screen.getByRole("button", { name: "Show password" }));
    expect(inputByLabel(/^password/i)).toHaveAttribute("type", "text");
    expect(screen.getByRole("button", { name: "Hide password" })).toHaveAttribute("aria-pressed", "true");

    await user.click(screen.getByRole("checkbox", { name: "Remember me" }));
    await user.click(screen.getByRole("button", { name: "Sign in" }));
    expect(await screen.findByRole("status")).toHaveTextContent(/nothing was sent, saved, or changed/i);
    expect(setItem).not.toHaveBeenCalled();
    expect(screen.getByRole("button", { name: "Continue with Google" })).toBeDisabled();
    expect(screen.getByRole("button", { name: "Continue with Facebook" })).toBeDisabled();
    setItem.mockRestore();
  });

  it("moves between screens and keeps focus on the heading", async () => {
    const user = userEvent.setup();
    renderAuth();
    await user.click(screen.getByRole("button", { name: "Create account" }));
    const heading = screen.getByRole("heading", { name: "Create your account" });
    expect(heading).toHaveFocus();

    await user.click(screen.getByRole("button", { name: "Back to sign in" }));
    await user.click(screen.getByRole("button", { name: "Forgot password?" }));
    expect(screen.getByRole("heading", { name: "Forgot password" })).toHaveFocus();
  });

  it("submits forgot and reset into honest review states", async () => {
    const user = userEvent.setup();
    const { rerender } = renderAuth({ screen: "forgot" });
    await user.type(inputByLabel(/^email/i), "ada@example.com");
    await user.keyboard("{Enter}");
    expect(await screen.findByRole("heading", { name: "Reset instructions" })).toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent(/no email was sent/i);

    rerender(<AuthExperience frame="preview" submitDelayMs={0} screen="reset" />);
    await user.type(inputByLabel(/^new password/i), "ledger-1947");
    await user.type(inputByLabel(/^confirm password/i), "ledger-1947");
    await user.click(screen.getByRole("button", { name: "Update password" }));
    expect(await screen.findByRole("heading", { name: "Password updated" })).toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent(/no password was changed/i);
  });

  it("shows seeded review states for errors, visibility, loading, and failure", () => {
    const { rerender } = renderAuth({ demonstration: "invalid" });
    expect(screen.getByText(/valid email/i)).toBeInTheDocument();
    expect(screen.getByText("Enter your password.")).toBeInTheDocument();

    rerender(<AuthExperience frame="preview" submitDelayMs={0} demonstration="visible" />);
    expect(inputByLabel(/^password/i)).toHaveAttribute("type", "text");

    rerender(<AuthExperience frame="preview" submitDelayMs={0} demonstration="hidden" />);
    expect(inputByLabel(/^password/i)).toHaveAttribute("type", "password");

    rerender(<AuthExperience frame="preview" submitDelayMs={0} demonstration="loading" />);
    expect(screen.getByRole("button", { name: "Sign in" })).toBeDisabled();
    expect(inputByLabel(/^email/i)).toBeDisabled();

    rerender(<AuthExperience frame="preview" submitDelayMs={0} demonstration="failure" />);
    expect(screen.getByRole("alert")).toHaveTextContent(/no account was checked/i);
  });

  it("checks register confirmation and password requirements", async () => {
    const user = userEvent.setup();
    renderAuth({ screen: "register", demonstration: "invalid" });
    expect(screen.getByText("Enter your name.")).toBeInTheDocument();
    expect(screen.getByText(/do not match/i)).toBeInTheDocument();
    expect(screen.getByRole("list", { name: "Password requirements" })).toBeInTheDocument();

    await user.clear(inputByLabel(/^name/i));
    await user.type(inputByLabel(/^name/i), "Ada Lovelace");
    await user.clear(inputByLabel(/^email/i));
    await user.type(inputByLabel(/^email/i), "ada@example.com");
    await user.clear(inputByLabel(/^password/i));
    await user.type(inputByLabel(/^password/i), "ledger-1947");
    await user.clear(inputByLabel(/^confirm password/i));
    await user.type(inputByLabel(/^confirm password/i), "ledger-1947");
    expect(screen.getAllByText("Met.").length).toBeGreaterThan(0);
    await user.click(screen.getByRole("button", { name: "Create account" }));
    expect(await screen.findByRole("status")).toHaveTextContent(/nothing was sent, saved, or changed/i);
  });
});
