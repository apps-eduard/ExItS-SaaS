import { useState } from "react";
import { describe, expect, it, beforeEach, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import type { PersonalDashboardDto } from "@/api/platform/personal-dashboard-client";
import {
  PERSONAL_HOME_KPI_TEST_IDS,
  PersonalHomeKpiStrip,
} from "@/features/personal/PersonalHomeKpiStrip";
import {
  PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY,
  type PersonalHomeSurfaceStyle,
} from "@/features/personal/personal-home-surface-style";

vi.mock("@/features/personal/use-personal-home-kpi-sparklines", () => ({
  usePersonalHomeKpiSparklines: vi.fn(() => ({
    series: {
      "my-record-lent": [],
      "my-record-owe": [],
      "shared-lent": [],
      "shared-owe": [],
    },
    periodOpening: {
      "my-record-lent": null,
      "my-record-owe": null,
      "shared-lent": null,
      "shared-owe": null,
    },
    activeCounts: {
      "my-record-lent": 0,
      "my-record-owe": 0,
      "shared-lent": 0,
      "shared-owe": 0,
    },
    loading: false,
  })),
}));

const dashboard: PersonalDashboardDto = {
  userIdentityId: "11111111-1111-1111-1111-111111111111",
  accountProfileId: "22222222-2222-2222-2222-222222222222",
  accountClass: "Personal",
  utangAvailable: true,
  contactCount: 1,
  activeRelationshipCount: 1,
  totalLentBalance: 100,
  totalBorrowedBalance: 50,
  sharedWithMeLentBalance: 0,
  sharedWithMeBorrowedBalance: 0,
  sharedWithMeActiveCount: 0,
  pendingConfirmationCount: 0,
};

function Harness({
  initial = "modern-solid" as PersonalHomeSurfaceStyle,
}: {
  initial?: PersonalHomeSurfaceStyle;
}) {
  const [surfaceStyle, setSurfaceStyle] = useState(initial);
  return (
    <div data-testid="surface-style-host" data-surface-style={surfaceStyle}>
      <PersonalHomeKpiStrip
        dashboard={dashboard}
        surfaceStyle={surfaceStyle}
        onSurfaceStyleChange={setSurfaceStyle}
      />
    </div>
  );
}

describe("Personal Home surface style control", () => {
  beforeEach(() => {
    window.localStorage.clear();
  });

  it("defaults Modern Solid beside Trend period and updates data-surface-style", async () => {
    const user = userEvent.setup();
    render(
      <AppProviders>
        <MemoryRouter>
          <Harness />
        </MemoryRouter>
      </AppProviders>,
    );

    expect(screen.getByTestId("surface-style-host")).toHaveAttribute(
      "data-surface-style",
      "modern-solid",
    );
    expect(screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.trendRange)).toBeInTheDocument();
    expect(screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.surfaceStyle)).toBeInTheDocument();
    expect(screen.getByText("Modern Solid")).toBeInTheDocument();

    await user.click(screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.surfaceStyle));
    await user.click(
      screen.getByTestId(`${PERSONAL_HOME_KPI_TEST_IDS.surfaceStyle}-option-ehg-glass`),
    );

    expect(screen.getByTestId("surface-style-host")).toHaveAttribute(
      "data-surface-style",
      "ehg-glass",
    );
    expect(screen.getByTestId(`${PERSONAL_HOME_KPI_TEST_IDS.surfaceStyle}-value`)).toHaveTextContent(
      "EHG Glass",
    );
  });

  it("does not write Preferences storage when changing surface style", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn((next: PersonalHomeSurfaceStyle) => {
      window.localStorage.setItem(PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY, next);
    });

    render(
      <AppProviders>
        <MemoryRouter>
          <PersonalHomeKpiStrip
            dashboard={dashboard}
            surfaceStyle="modern-solid"
            onSurfaceStyleChange={onChange}
          />
        </MemoryRouter>
      </AppProviders>,
    );

    await user.click(screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.surfaceStyle));
    await user.click(
      screen.getByTestId(`${PERSONAL_HOME_KPI_TEST_IDS.surfaceStyle}-option-ehg-glass`),
    );

    expect(onChange).toHaveBeenCalledWith("ehg-glass");
    expect(window.localStorage.getItem(PERSONAL_HOME_SURFACE_STYLE_STORAGE_KEY)).toBe("ehg-glass");
    expect(window.localStorage.getItem("exits.pos-client.ui-preferences.v1")).toBeNull();
  });
});
