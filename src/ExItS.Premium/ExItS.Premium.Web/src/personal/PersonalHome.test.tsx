import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AppearanceProvider } from "../theme/AppearanceProvider";
import { PersonalHome } from "./PersonalHome";
import type { HomeSnapshot } from "./home-data";

const empty: HomeSnapshot = {
  lent: [],
  borrowed: [],
  upcoming: [],
  attention: [],
  activity: [],
  tasks: [],
  stores: [],
  unavailable: {},
};

describe("personal home", () => {
  it("greets the signed-in person and keeps unfinished areas quiet", async () => {
    const user = userEvent.setup();
    const onLogout = vi.fn();
    render(
      <AppearanceProvider>
        <PersonalHome name="Ada Lovelace" email="ada@example.com" snapshot={empty} onLogout={onLogout} />
      </AppearanceProvider>,
    );

    expect(screen.getByRole("heading", { name: "Hello, Ada Lovelace" })).toBeInTheDocument();
    expect(screen.getByText("Nothing needs attention right now.")).toBeInTheDocument();
    expect(screen.getAllByText("None yet")).toHaveLength(2);
    expect(screen.getByText("No activity yet.")).toBeInTheDocument();
    expect(screen.getByText("No upcoming tasks.")).toBeInTheDocument();
    expect(screen.queryByText(/\/api\//)).not.toBeInTheDocument();

    await user.click(screen.getAllByRole("button", { name: "Utang" })[0]);
    expect(screen.getByRole("status")).toHaveTextContent(/not available yet/i);

    await user.click(screen.getByRole("button", { name: "Account menu for Ada Lovelace" }));
    await user.click(screen.getByRole("button", { name: "Log out" }));
    expect(onLogout).toHaveBeenCalledOnce();
  });
});
