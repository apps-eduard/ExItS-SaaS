import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ConnectivityNotice } from "@/connectivity/ConnectivityNotice";
import { OnlineRequiredBoot, OnlineRequiredPageState } from "@/components/exits/OnlineRequiredBoot";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";

function wrap(ui: React.ReactNode) {
  return (
    <PreferencesProvider>
      <I18nProvider>{ui}</I18nProvider>
    </PreferencesProvider>
  );
}

describe("Organization connectivity UX", () => {
  it("shows offline banner while connection is lost", () => {
    render(wrap(<ConnectivityNotice offline reconnecting />));
    expect(screen.getByTestId("connectivity-notice")).toBeInTheDocument();
    expect(screen.getByText("You're offline")).toBeInTheDocument();
    expect(screen.getByText("Reconnecting…")).toBeInTheDocument();
  });

  it("shows back-online flash", () => {
    render(wrap(<ConnectivityNotice offline={false} backOnline />));
    expect(screen.getByTestId("connectivity-back-online")).toBeInTheDocument();
  });

  it("renders OnlineRequired boot without endless spinner", () => {
    render(wrap(<OnlineRequiredBoot onRetry={() => undefined} />));
    expect(screen.getByTestId("online-required-boot")).toBeInTheDocument();
    expect(screen.getByTestId("online-required-boot-retry")).toBeInTheDocument();
    expect(screen.queryByTestId("app-boot-loader")).not.toBeInTheDocument();
  });

  it("renders page-level OnlineRequired", () => {
    render(wrap(<OnlineRequiredPageState title="Inventory" />));
    expect(screen.getByTestId("online-required-page")).toBeInTheDocument();
    expect(screen.getByText("Inventory")).toBeInTheDocument();
  });
});
