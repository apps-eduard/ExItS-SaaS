import { describe, expect, it, vi } from "vitest";
import { act, render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { useState } from "react";
import { SideDrawer } from "@/components/exits/SideDrawer";
import { PreferencesProvider } from "@/hooks/usePreferences";
import { I18nProvider } from "@/i18n/I18nProvider";

function Probe({ initialOpen = false }: { initialOpen?: boolean }) {
  const [open, setOpen] = useState(initialOpen);
  return (
    <>
      <button type="button" data-testid="open" onClick={() => setOpen(true)}>
        Open
      </button>
      <button type="button" data-testid="close" onClick={() => setOpen(false)}>
        Close
      </button>
      <button type="button" data-testid="under-target">
        Under
      </button>
      <SideDrawer open={open} onClose={() => setOpen(false)} title="T" testId="probe-drawer">
        <p data-testid="probe-body">Body</p>
      </SideDrawer>
    </>
  );
}

function renderProbe(initialOpen = false) {
  return render(
    <PreferencesProvider>
      <I18nProvider>
        <Probe initialOpen={initialOpen} />
      </I18nProvider>
    </PreferencesProvider>,
  );
}

describe("SideDrawer open toggle", () => {
  it("mounts portal after open false -> true", async () => {
    const user = userEvent.setup();
    renderProbe();
    expect(screen.queryByTestId("probe-drawer")).not.toBeInTheDocument();
    await user.click(screen.getByTestId("open"));
    await waitFor(() => {
      expect(screen.getByTestId("probe-drawer")).toBeInTheDocument();
      expect(screen.getByTestId("probe-body")).toBeInTheDocument();
    });
  });

  it("drops backdrop interactivity immediately on close (no click steal)", async () => {
    vi.useFakeTimers({ shouldAdvanceTime: true });
    const user = userEvent.setup({ advanceTimers: vi.advanceTimersByTime });
    renderProbe();

    await user.click(screen.getByTestId("open"));
    await waitFor(() => {
      expect(screen.getByTestId("probe-drawer-backdrop")).toHaveAttribute(
        "data-interactive",
        "true",
      );
    });

    await user.click(screen.getByTestId("close"));
    expect(screen.getByTestId("probe-drawer-backdrop")).toHaveAttribute(
      "data-interactive",
      "false",
    );
    expect(screen.getByTestId("probe-drawer")).toHaveAttribute("inert");

    // Rapid reopen/close must not leave an interactive ghost backdrop.
    await user.click(screen.getByTestId("open"));
    await user.click(screen.getByTestId("close"));
    expect(screen.getByTestId("probe-drawer-backdrop")).toHaveAttribute(
      "data-interactive",
      "false",
    );
    expect(screen.getByTestId("probe-drawer")).toHaveAttribute("inert");

    await act(async () => {
      vi.advanceTimersByTime(600);
    });
    await waitFor(() => {
      expect(screen.queryByTestId("probe-drawer")).not.toBeInTheDocument();
    });

    vi.useRealTimers();
  });
});
