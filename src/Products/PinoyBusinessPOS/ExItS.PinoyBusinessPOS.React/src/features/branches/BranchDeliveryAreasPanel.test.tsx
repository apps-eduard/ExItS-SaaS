import { describe, expect, it, vi, beforeEach, afterEach } from "vitest";
import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BranchDeliveryAreasPanel } from "@/features/branches/BranchDeliveryAreasPanel";
import type { BranchDeliveryServiceAreaDto } from "@/api/platform/branch-fulfillment-client";
import type { MessageKey } from "@/i18n/messages";
import { en } from "@/i18n/locales/en";

vi.mock("@/api/platform/ph-locality-client", () => ({
  listPhilippineRegions: vi.fn(),
  listPhilippineLocalitiesByRegion: vi.fn(),
  searchPhilippineLocalities: vi.fn(),
}));

import {
  listPhilippineLocalitiesByRegion,
  listPhilippineRegions,
} from "@/api/platform/ph-locality-client";

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

const NIR = {
  regionCode: "1800000000",
  regionName: "Negros Island Region (NIR)",
};

const NCR = {
  regionCode: "1300000000",
  regionName: "National Capital Region (NCR)",
};

const BACOLOD = {
  psgcCode: "1830200000",
  name: "City of Bacolod",
  localityType: "City",
  regionCode: "1800000000",
  regionName: "Negros Island Region (NIR)",
  provinceCode: null,
  provinceName: null,
  displayLabel: "Bacolod City · Negros Island Region (NIR)",
};

const MURCIA = {
  psgcCode: "1804520000",
  name: "Murcia",
  localityType: "Municipality",
  regionCode: "1800000000",
  regionName: "Negros Island Region (NIR)",
  provinceCode: "1804500000",
  provinceName: "Negros Occidental",
  displayLabel: "Murcia · Negros Occidental",
};

async function chooseRegion(user: ReturnType<typeof userEvent.setup>) {
  await waitFor(() => expect(screen.getByTestId(`delivery-area-region-${NIR.regionCode}`)).toBeVisible());
  await user.click(screen.getByTestId(`delivery-area-region-${NIR.regionCode}`));
  await waitFor(() => expect(screen.getByTestId("delivery-area-city-list")).toBeVisible());
  expect(screen.queryByTestId("delivery-area-region-list")).not.toBeInTheDocument();
  expect(screen.getByTestId("delivery-area-region-selected")).toHaveTextContent(NIR.regionName);
}

describe("BranchDeliveryAreasPanel PSGC", () => {
  beforeEach(() => {
    vi.mocked(listPhilippineRegions).mockReset();
    vi.mocked(listPhilippineLocalitiesByRegion).mockReset();
    vi.mocked(listPhilippineRegions).mockResolvedValue([NIR, NCR]);
    vi.mocked(listPhilippineLocalitiesByRegion).mockResolvedValue([BACOLOD, MURCIA]);
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("keeps city list closed until a region is chosen", async () => {
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
    expect(screen.getByTestId("delivery-area-region-search")).toBeInTheDocument();
    expect(screen.getByTestId("delivery-area-pick-region")).toBeVisible();
    expect(screen.queryByTestId("delivery-area-city-list")).not.toBeInTheDocument();
    await waitFor(() => expect(listPhilippineRegions).toHaveBeenCalled());
  });

  it("filters regions by search then loads cities", async () => {
    const user = userEvent.setup();

    render(
      <BranchDeliveryAreasPanel
        areas={[]}
        busy={false}
        t={t}
        onAdd={async () => undefined}
        onRemove={async () => undefined}
      />,
    );

    await waitFor(() => expect(screen.getByTestId(`delivery-area-region-${NIR.regionCode}`)).toBeVisible());
    await user.type(screen.getByTestId("delivery-area-region-search"), "negros");
    expect(screen.getByTestId(`delivery-area-region-${NIR.regionCode}`)).toBeVisible();
    expect(screen.queryByTestId(`delivery-area-region-${NCR.regionCode}`)).not.toBeInTheDocument();

    await user.click(screen.getByTestId(`delivery-area-region-${NIR.regionCode}`));
    await waitFor(() => expect(listPhilippineLocalitiesByRegion).toHaveBeenCalledWith(NIR.regionCode, expect.anything()));
    expect(screen.getByTestId("delivery-area-region-selected")).toHaveTextContent(NIR.regionName);
    expect(screen.queryByTestId("delivery-area-region-list")).not.toBeInTheDocument();
  });

  it("supports city search, check closes list, reopen to multi-add", async () => {
    const user = userEvent.setup();
    const onAdd = vi.fn(async () => undefined);

    render(
      <BranchDeliveryAreasPanel
        areas={[]}
        busy={false}
        t={t}
        onAdd={onAdd}
        onRemove={async () => undefined}
      />,
    );

    await chooseRegion(user);

    await user.type(screen.getByTestId("delivery-area-city-search"), "mur");
    expect(screen.getByTestId("delivery-area-city-1804520000")).toBeVisible();
    expect(screen.queryByTestId("delivery-area-city-1830200000")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("delivery-area-city-1804520000").querySelector("input")!);
    expect(onAdd).toHaveBeenCalledWith(MURCIA.psgcCode);
    expect(screen.queryByTestId("delivery-area-city-list")).not.toBeInTheDocument();

    await user.click(screen.getByTestId("delivery-area-city-search"));
    await waitFor(() => expect(screen.getByTestId("delivery-area-city-list")).toBeVisible());
    await user.clear(screen.getByTestId("delivery-area-city-search"));
    await user.click(screen.getByTestId("delivery-area-city-1830200000").querySelector("input")!);
    expect(onAdd).toHaveBeenCalledWith(BACOLOD.psgcCode);
    expect(onAdd).toHaveBeenCalledTimes(2);
    expect(screen.queryByTestId("delivery-area-city-list")).not.toBeInTheDocument();
  });

  it("toggles individual cities and keeps already selected checked when reopened", async () => {
    const user = userEvent.setup();
    const onAdd = vi.fn(async () => undefined);

    render(
      <BranchDeliveryAreasPanel
        areas={[area({})]}
        busy={false}
        t={t}
        onAdd={onAdd}
        onRemove={async () => undefined}
      />,
    );

    expect(screen.getByTestId("delivery-areas-list")).toHaveTextContent("Bacolod City");
    await chooseRegion(user);
    expect(screen.getByTestId("delivery-area-city-1830200000").querySelector("input")).toBeChecked();

    await user.click(screen.getByTestId("delivery-area-city-1804520000").querySelector("input")!);
    expect(onAdd).toHaveBeenCalledWith(MURCIA.psgcCode);
    expect(screen.queryByTestId("delivery-area-city-list")).not.toBeInTheDocument();
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
