import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { AppProviders } from "@/app/providers";
import { ConnectivityNotice } from "@/connectivity/ConnectivityNotice";
import { UI_PREFERENCES_STORAGE_KEY } from "@/lib/preferences/ui-preferences";

describe("connectivity notice", () => {
  it("stays hidden while the browser reports online", () => {
    render(
      <AppProviders>
        <ConnectivityNotice offline={false} />
      </AppProviders>,
    );
    expect(screen.queryByTestId("connectivity-notice")).not.toBeInTheDocument();
  });

  it("shows English advisory copy when offline and does not claim Offline POS mode", () => {
    render(
      <AppProviders>
        <ConnectivityNotice offline />
      </AppProviders>,
    );
    expect(screen.getByTestId("connectivity-notice")).toHaveTextContent("You're offline");
    expect(screen.getByTestId("connectivity-notice")).toHaveTextContent(
      "Shown data may be out of date until you reconnect.",
    );
    expect(screen.getByTestId("connectivity-notice")).not.toHaveTextContent(/offline pos mode/i);
    expect(screen.getByTestId("connectivity-notice")).not.toHaveTextContent(/authenticated/i);
  });

  it("shows Filipino advisory copy", () => {
    window.localStorage.setItem(
      UI_PREFERENCES_STORAGE_KEY,
      JSON.stringify({ theme: "light", locale: "fil-PH" }),
    );
    render(
      <AppProviders>
        <ConnectivityNotice offline />
      </AppProviders>,
    );
    expect(screen.getByTestId("connectivity-notice")).toHaveTextContent("Wala kang koneksyon");
    expect(screen.getByTestId("connectivity-notice")).toHaveTextContent(
      "Maaaring luma ang ipinapakitang data hanggang makakonekta ka ulit.",
    );
  });
});
