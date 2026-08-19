import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import { AppTopBar } from "@/components/exits/AppTopBar";
import { getBrowserNetworkReachability } from "@/adapters/connectivity";

describe("connectivity foundation", () => {
  it("does not treat navigator.onLine as API-health Online status", () => {
    expect(getBrowserNetworkReachability()).not.toBe("Online");
    expect(["unknown", true, false]).toContain(getBrowserNetworkReachability());
  });

  it("AppTopBar does not claim Online or Offline", () => {
    render(
      <AppProviders>
        <MemoryRouter>
          <AppTopBar />
        </MemoryRouter>
      </AppProviders>,
    );
    const header = screen.getByRole("banner");
    expect(header).toHaveTextContent("ExItS Mobile");
    expect(header).not.toHaveTextContent("Online");
    expect(header).not.toHaveTextContent("Offline");
    expect(header).not.toHaveTextContent("Syncing");
  });
});
