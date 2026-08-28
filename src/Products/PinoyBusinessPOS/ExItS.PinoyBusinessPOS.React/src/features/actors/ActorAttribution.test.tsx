import type { ReactNode } from "react";
import { describe, expect, it } from "vitest";
import { render, screen } from "@testing-library/react";
import { ActorAttribution } from "@/features/actors/ActorAttribution";
import { I18nProvider } from "@/i18n/I18nProvider";
import { PreferencesProvider } from "@/hooks/usePreferences";

const actorId = "aaaaaaaa-aaaa-4aaa-8aaa-aaaaaaaaaaaa";

function wrap(ui: ReactNode) {
  return (
    <PreferencesProvider>
      <I18nProvider>{ui}</I18nProvider>
    </PreferencesProvider>
  );
}

describe("ActorAttribution", () => {
  it("renders resolved display name and never shows the raw GUID", () => {
    render(
      wrap(
        <ActorAttribution
          labelKey="common.soldBy"
          actorId={actorId}
          occurredAtUtc="2026-08-21T02:00:00Z"
          resolved={{
            actorId,
            displayName: "Maria Santos",
            actorStatus: "Active",
          }}
        />,
      ),
    );

    expect(screen.getByTestId("actor-attribution-name")).toHaveTextContent("Maria Santos");
    expect(screen.queryByText(actorId)).not.toBeInTheDocument();
    expect(screen.getByText("Sold by")).toBeInTheDocument();
  });

  it("shows Not available instead of a GUID when unresolved", () => {
    render(
      wrap(
        <ActorAttribution
          labelKey="common.recordedBy"
          actorId={actorId}
          resolved={null}
          isLoading={false}
        />,
      ),
    );

    expect(screen.getByTestId("actor-attribution-name")).toHaveTextContent("Not available");
    expect(screen.queryByText(actorId)).not.toBeInTheDocument();
  });
});
