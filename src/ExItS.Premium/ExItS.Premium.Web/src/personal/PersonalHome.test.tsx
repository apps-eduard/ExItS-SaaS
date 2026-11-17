import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { AppearanceProvider } from "../theme/AppearanceProvider";
import { PersonalHome } from "./PersonalHome";
import type { HomeSnapshot } from "./home-data";

const empty: HomeSnapshot = {
  lent: [],
  borrowed: [],
  sharedLent: [],
  sharedBorrowed: [],
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

    expect(screen.getByText("Personal Home")).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: /Ada Lovelace/ })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Personal Tracker" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "My Records" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Shared with Me" })).toBeInTheDocument();
    expect(screen.getByText("Nothing needs attention right now.")).toBeInTheDocument();
    expect(screen.getAllByText("No records")).toHaveLength(4);
    expect(screen.getByText("No stores linked")).toBeInTheDocument();
    expect(screen.getByText("No activity yet.")).toBeInTheDocument();
    expect(screen.getByText("No upcoming tasks.")).toBeInTheDocument();
    expect(screen.queryByText(/\/api\//)).not.toBeInTheDocument();
    expect(screen.getAllByText("Coming soon")).toHaveLength(4);
    expect(screen.queryByText("Read only")).not.toBeInTheDocument();
    expect(screen.getByText("Owned by someone else · read only.")).toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /I Lent/i }));
    expect(screen.getByRole("status")).toHaveTextContent(/not available yet/i);
    expect(screen.getByRole("status")).toHaveTextContent(/Nothing was saved/i);

    await user.click(screen.getByRole("button", { name: "Account menu for Ada Lovelace" }));
    expect(screen.getByRole("menu", { name: "Account menu for Ada Lovelace" })).toBeInTheDocument();
    expect(screen.getByText("Personal")).toBeInTheDocument();
    expect(screen.getByRole("menuitem", { name: /Appearance/i })).toBeInTheDocument();
    await user.click(screen.getByRole("menuitem", { name: /Sign out/i }));
    expect(onLogout).toHaveBeenCalledOnce();
  });

  it("shows real attention and activity details without inventing balances", () => {
    const snapshot: HomeSnapshot = {
      ...empty,
      lent: [{ currency: "PHP", amount: 40 }],
      attention: ["1 payment confirmation waiting."],
      upcoming: [{ id: "u1", label: "Money lent · ₱40.00", when: "2026-12-01T00:00:00Z" }],
      activity: [{ id: "a1", title: "Money lent", detail: "₱40.00 · Active", at: "2026-11-01T00:00:00Z" }],
      tasks: [{ id: "t1", title: "Call store", dueAtUtc: "2026-11-20T00:00:00Z", status: "Open" }],
    };

    render(
      <AppearanceProvider>
        <PersonalHome name="Ada Lovelace" email="ada@example.com" snapshot={snapshot} onLogout={() => undefined} />
      </AppearanceProvider>,
    );

    expect(screen.getByText("1 payment confirmation waiting.")).toBeInTheDocument();
    expect(screen.getByText("₱40.00 · Active")).toBeInTheDocument();
    expect(screen.getByText("Call store")).toBeInTheDocument();
    expect(screen.getByText("Open")).toBeInTheDocument();
    expect(screen.getByText("₱40.00")).toBeInTheDocument();
  });

  it("keeps Shared with Me as its own ownership group with one group-level read-only badge", () => {
    render(
      <AppearanceProvider>
        <PersonalHome
          name="Ada Lovelace"
          email="ada@example.com"
          snapshot={{
            ...empty,
            lent: [{ currency: "PHP", amount: 40 }],
            borrowed: [{ currency: "PHP", amount: 0 }],
            sharedLent: [{ currency: "USD", amount: 8 }],
            sharedBorrowed: [{ currency: "PHP", amount: 7705000 }],
          }}
          onLogout={() => undefined}
        />
      </AppearanceProvider>,
    );

    expect(screen.getByRole("heading", { name: "My Records" })).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "Shared with Me" })).toBeInTheDocument();
    expect(screen.getByText("Records you own, including ones you shared.")).toBeInTheDocument();
    expect(screen.getByText("Owned by someone else · read only.")).toBeInTheDocument();
    expect(screen.getAllByText("Read only")).toHaveLength(1);
    expect(screen.getByText("₱40.00")).toBeInTheDocument();
    expect(screen.getByText("₱0.00")).toBeInTheDocument();
    expect(screen.getByText(/\$8\.00|USD/)).toBeInTheDocument();
    expect(screen.queryByText(/Shared with me: lent/)).not.toBeInTheDocument();
    expect(screen.queryByText(/Not included above/)).not.toBeInTheDocument();
  });

  it("keeps group read-only copy without badge spam on empty shared metrics", () => {
    render(
      <AppearanceProvider>
        <PersonalHome
          name="Ada Lovelace"
          email="ada@example.com"
          snapshot={{
            ...empty,
            sharedBorrowed: [{ currency: "PHP", amount: 100 }],
          }}
          onLogout={() => undefined}
        />
      </AppearanceProvider>,
    );

    expect(screen.getByText("Owned by someone else · read only.")).toBeInTheDocument();
    expect(screen.getAllByText("Read only")).toHaveLength(1);
    expect(screen.getByText("₱100.00")).toBeInTheDocument();
    expect(screen.getAllByText("No records")).toHaveLength(3);
  });
});
