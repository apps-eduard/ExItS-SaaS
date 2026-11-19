import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router-dom";
import { AppProviders } from "@/app/providers";
import type { PersonalDashboardDto } from "@/api/platform/personal-dashboard-client";
import {
  PERSONAL_HOME_KPI_TEST_IDS,
  PersonalHomeKpiStrip,
  formatPersonalHomeKpiActiveMeta,
} from "@/features/personal/PersonalHomeKpiStrip";
import { en } from "@/i18n/locales/en";
import type { MessageKey } from "@/i18n/messages";

const t = (key: MessageKey) => en[key];
import * as sparklines from "@/features/personal/use-personal-home-kpi-sparklines";

const defaultActiveCounts = {
  "my-record-lent": 2,
  "my-record-owe": 0,
  "shared-lent": 1,
  "shared-owe": 0,
} as const;

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
    activeCounts: { ...defaultActiveCounts },
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

function renderStrip() {
  return render(
    <AppProviders>
      <MemoryRouter>
        <PersonalHomeKpiStrip dashboard={dashboard} />
      </MemoryRouter>
    </AppProviders>,
  );
}

describe("formatPersonalHomeKpiActiveMeta", () => {
  it("uses singular, plural, and zero copy for my records", () => {
    expect(formatPersonalHomeKpiActiveMeta(t, "my-record", 0)).toBe("No active records");
    expect(formatPersonalHomeKpiActiveMeta(t, "my-record", 1)).toBe("1 active record");
    expect(formatPersonalHomeKpiActiveMeta(t, "my-record", 3)).toBe("3 active records");
  });

  it("uses singular, plural, and zero copy for shared with me", () => {
    expect(formatPersonalHomeKpiActiveMeta(t, "shared", 0)).toBe("No active shared records");
    expect(formatPersonalHomeKpiActiveMeta(t, "shared", 1)).toBe("1 active shared record");
    expect(formatPersonalHomeKpiActiveMeta(t, "shared", 2)).toBe("2 active shared records");
  });
});

describe("PersonalHomeKpiStrip trend range", () => {
  it("shows Org EHG preset select with default Last 3 months", () => {
    renderStrip();
    expect(screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.trendRange)).toBeInTheDocument();
    expect(screen.getByText("Last 3 months")).toBeInTheDocument();
    expect(screen.queryByTestId("personal-home-kpi-custom-range")).not.toBeInTheDocument();
  });

  it("uses Soft Glass date pickers for custom from and to", async () => {
    const user = userEvent.setup();
    renderStrip();
    await user.click(screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.trendRange));
    await user.click(screen.getByTestId(`${PERSONAL_HOME_KPI_TEST_IDS.trendRange}-option-custom`));

    const custom = screen.getByTestId("personal-home-kpi-custom-range");
    expect(custom).toBeInTheDocument();
    expect(screen.getByTestId("personal-home-kpi-custom-from")).toHaveClass("exits-sg-datetime");
    expect(screen.getByTestId("personal-home-kpi-custom-to")).toHaveClass("exits-sg-datetime");
    expect(screen.queryByTestId("personal-home-kpi-custom-from-input")).not.toHaveAttribute(
      "type",
      "date",
    );
    expect(vi.mocked(sparklines.usePersonalHomeKpiSparklines)).toHaveBeenCalled();
  });

  it("shows period trend badges from sparkline series", () => {
    vi.mocked(sparklines.usePersonalHomeKpiSparklines).mockReturnValueOnce({
      series: {
        "my-record-lent": [100, 150],
        "my-record-owe": [80, 40],
        "shared-lent": [0, 50],
        "shared-owe": [40, 40],
      },
      periodOpening: {
        "my-record-lent": 100,
        "my-record-owe": 80,
        "shared-lent": 0,
        "shared-owe": 40,
      },
      activeCounts: { ...defaultActiveCounts },
      loading: false,
    });
    renderStrip();

    const myLentTrend = screen.getByTestId(`${PERSONAL_HOME_KPI_TEST_IDS.myRecordLent}-trend`);
    expect(myLentTrend).toHaveTextContent("↑ +50.0%");
    expect(myLentTrend).toHaveClass("personal-home-kpi-card__trend--good");

    const myOweTrend = screen.getByTestId(`${PERSONAL_HOME_KPI_TEST_IDS.myRecordOwe}-trend`);
    expect(myOweTrend).toHaveTextContent("↓ -50.0%");
    expect(myOweTrend).toHaveClass("personal-home-kpi-card__trend--good");

    expect(
      screen.queryByTestId(`${PERSONAL_HOME_KPI_TEST_IDS.sharedLent}-trend`),
    ).not.toBeInTheDocument();

    const sharedOweTrend = screen.getByTestId(`${PERSONAL_HOME_KPI_TEST_IDS.sharedOwe}-trend`);
    expect(sharedOweTrend).toHaveTextContent("→ 0.0%");
    expect(sharedOweTrend).toHaveClass("personal-home-kpi-card__trend--neutral");
  });

  it("hides period trend when series data is unavailable", () => {
    vi.mocked(sparklines.usePersonalHomeKpiSparklines).mockReturnValueOnce({
      series: {
        "my-record-lent": null,
        "my-record-owe": null,
        "shared-lent": null,
        "shared-owe": null,
      },
      periodOpening: {
        "my-record-lent": null,
        "my-record-owe": null,
        "shared-lent": null,
        "shared-owe": null,
      },
      activeCounts: { ...defaultActiveCounts },
      loading: false,
    });
    renderStrip();

    expect(
      screen.queryByTestId(`${PERSONAL_HOME_KPI_TEST_IDS.myRecordLent}-trend`),
    ).not.toBeInTheDocument();
  });

  it("shows primary lent/owe titles, secondary scope labels, and active-record meta", () => {
    renderStrip();

    const myLent = screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.myRecordLent);
    expect(myLent).toHaveTextContent("I Lent");
    expect(myLent).toHaveTextContent("My Records");
    expect(myLent).toHaveTextContent("2 active records");
    expect(myLent).not.toHaveTextContent("pending confirmation");

    const myOwe = screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.myRecordOwe);
    expect(myOwe).toHaveTextContent("I Owe");
    expect(myOwe).toHaveTextContent("My Records");
    expect(myOwe).toHaveTextContent("No active records");

    const sharedLent = screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.sharedLent);
    expect(sharedLent).toHaveTextContent("I Lent");
    expect(sharedLent).toHaveTextContent("Shared with Me");
    expect(sharedLent).toHaveTextContent("1 active shared record");

    const sharedOwe = screen.getByTestId(PERSONAL_HOME_KPI_TEST_IDS.sharedOwe);
    expect(sharedOwe).toHaveTextContent("I Owe");
    expect(sharedOwe).toHaveTextContent("Shared with Me");
    expect(sharedOwe).toHaveTextContent("No active shared records");
  });
});