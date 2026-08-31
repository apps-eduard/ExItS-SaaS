import { describe, expect, it, vi, beforeEach, afterEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BranchDeliveryAreasPanel } from "@/features/branches/BranchDeliveryAreasPanel";
import type { BranchDeliveryServiceAreaDto } from "@/api/platform/branch-fulfillment-client";
import type { MessageKey } from "@/i18n/messages";
import { en } from "@/i18n/locales/en";

vi.mock("@/api/platform/ph-locality-client", () => ({
  searchPhilippineLocalities: vi.fn(),
}));

import { searchPhilippineLocalities } from "@/api/platform/ph-locality-client";

const t = (key: MessageKey) => en[key] ?? key;

function area(partial: Partial<BranchDeliveryServiceAreaDto>): BranchDeliveryServiceAreaDto {
  return {
    id: "area-1",
    organizationId: "org",
    branchId: "branch",
    countryCode: "PH",
    regionOrProvinceName: "Negros Occidental",
    cityMunicipalityName: "City of Bacolod",
    normalizedCityMunicipalityName: "CITY OF BACOLOD",
    psgcCode: "1830200000",
    localityType: "City",
    regionCode: "1800000000",
    regionName: "Negros Island Region (NIR)",
    provinceCode: null,
    provinceName: null,
    displayLabel: "Bacolod City · Negros Island Region (NIR)",
    isActive: true,
    isVerified: true,
    createdAtUtc: "2026-08-31T00:00:00Z",
    updatedAtUtc: "2026-08-31T00:00:00Z",
    ...partial,
  };
}

describe("BranchDeliveryAreasPanel PSGC", () => {
  beforeEach(() => {
    vi.mocked(searchPhilippineLocalities).mockReset();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("renders Philippines read-only and searchable selector without free-text city/province", async () => {
    render(
      <BranchDeliveryAreasPanel
        areas={[]}
        busy={false}
        t={t}
        onAdd={async () => undefined}
        onRemove={async () => undefined}
      />,
    );

    expect(screen.getByTestId("delivery-area-country-readonly")).toHaveTextContent("Philippines (PH)");
    expect(screen.getByTestId("delivery-area-search")).toBeInTheDocument();
    expect(screen.queryByTestId("delivery-area-city")).not.toBeInTheDocument();
    expect(screen.queryByTestId("delivery-area-region")).not.toBeInTheDocument();
    expect(screen.queryByTestId("delivery-area-country")).not.toBeInTheDocument();
    expect(screen.queryByTestId("add-delivery-area")).not.toBeInTheDocument();
  });

  it("searches and adds a locality via chip flow", async () => {
    const user = userEvent.setup();
    const onAdd = vi.fn(async () => undefined);
    vi.mocked(searchPhilippineLocalities).mockResolvedValue([
      {
        psgcCode: "1830200000",
        name: "City of Bacolod",
        localityType: "City",
        regionCode: "1800000000",
        regionName: "Negros Island Region (NIR)",
        provinceCode: null,
        provinceName: null,
        displayLabel: "Bacolod City · Negros Island Region (NIR)",
      },
      {
        psgcCode: "1804520000",
        name: "Murcia",
        localityType: "Municipality",
        regionCode: "1800000000",
        regionName: "Negros Island Region (NIR)",
        provinceCode: "1804500000",
        provinceName: "Negros Occidental",
        displayLabel: "Murcia · Negros Occidental",
      },
    ]);

    render(
      <BranchDeliveryAreasPanel
        areas={[]}
        busy={false}
        t={t}
        onAdd={onAdd}
        onRemove={async () => undefined}
      />,
    );

    await user.type(screen.getByTestId("delivery-area-search"), "bacolod");
    await waitFor(() => expect(searchPhilippineLocalities).toHaveBeenCalled());
    await waitFor(() => expect(screen.getByTestId("delivery-area-result-1830200000")).toBeVisible());
    expect(screen.getByTestId("delivery-area-result-1830200000")).toHaveTextContent("Bacolod City");
    expect(screen.getByTestId("delivery-area-result-1830200000")).toHaveTextContent("City");
    await user.click(screen.getByTestId("delivery-area-result-1830200000"));
    expect(onAdd).toHaveBeenCalledWith("1830200000");
  });

  it("shows selected chips and blocks already-added results", async () => {
    const user = userEvent.setup();
    vi.mocked(searchPhilippineLocalities).mockResolvedValue([
      {
        psgcCode: "1830200000",
        name: "City of Bacolod",
        localityType: "City",
        regionCode: "1800000000",
        regionName: "Negros Island Region (NIR)",
        provinceCode: null,
        provinceName: null,
        displayLabel: "Bacolod City · Negros Island Region (NIR)",
      },
    ]);

    render(
      <BranchDeliveryAreasPanel
        areas={[area({})]}
        busy={false}
        t={t}
        onAdd={async () => undefined}
        onRemove={async () => undefined}
      />,
    );

    expect(screen.getByTestId("delivery-areas-list")).toHaveTextContent("Bacolod City");
    await user.type(screen.getByTestId("delivery-area-search"), "bacolod");
    await waitFor(() => expect(screen.getByTestId("delivery-area-result-1830200000")).toBeDisabled());
    expect(screen.getByTestId("delivery-area-result-1830200000")).toHaveTextContent("Already added");
  });

  it("marks legacy unverified areas", () => {
    render(
      <BranchDeliveryAreasPanel
        areas={[
          area({
            id: "legacy",
            psgcCode: null,
            isVerified: false,
            cityMunicipalityName: "Bacolod City",
            displayLabel: "Bacolod City",
          }),
        ]}
        busy={false}
        t={t}
        onAdd={async () => undefined}
        onRemove={async () => undefined}
        onReplace={async () => undefined}
      />,
    );

    expect(screen.getByTestId("delivery-area-unverified-legacy")).toHaveTextContent("Needs verification");
    expect(screen.getByTestId("replace-delivery-area-legacy")).toBeVisible();
  });
});
