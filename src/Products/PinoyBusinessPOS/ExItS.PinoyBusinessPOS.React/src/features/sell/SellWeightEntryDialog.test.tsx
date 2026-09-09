import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { AppProviders } from "@/app/providers";
import type { PosCatalogProductDto } from "@/api/pos/pos-catalog-types";
import { SellWeightEntryDialog } from "@/features/sell/SellWeightEntryDialog";
import { formatPeso } from "@/lib/format-money";

const product: PosCatalogProductDto = {
  productId: "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  organizationId: "11111111-1111-1111-1111-111111111111",
  name: "Dried Fish",
  sku: "DF-1",
  unitOfMeasure: "Kilogram",
  sellingMode: "ByWeight",
  sellingPrice: 220,
  status: "Active",
  isTracked: true,
  onHandQuantity: 5,
  stockStatus: "InStock",
  createdAtUtc: "",
  updatedAtUtc: "",
  units: [],
};

describe("SellWeightEntryDialog", () => {
  beforeEach(() => {
    vi.stubGlobal("matchMedia", (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => undefined,
      removeListener: () => undefined,
      addEventListener: () => undefined,
      removeEventListener: () => undefined,
      dispatchEvent: () => false,
    }));
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it("keeps Add to cart disabled until a valid weight is entered", async () => {
    const user = userEvent.setup();
    const onConfirm = vi.fn();
    render(
      <AppProviders>
        <SellWeightEntryDialog
          open
          product={product}
          stockHint={{ isTracked: true, onHandQuantity: 5 }}
          onConfirm={onConfirm}
          onCancel={() => undefined}
        />
      </AppProviders>,
    );

    expect(screen.getByTestId("sell-weight-confirm")).toBeDisabled();
    expect(screen.getByTestId("sell-stock-hint")).toHaveTextContent("On hand: 5 kg");

    await user.type(screen.getByTestId("sell-weight-input"), "1.25");
    expect(screen.getByTestId("sell-weight-confirm")).toBeEnabled();
    expect(screen.getByTestId("sell-weight-preview")).toHaveTextContent(formatPeso(275));
  });

  it("converts grams and blocks over-stock entries", async () => {
    const user = userEvent.setup();
    render(
      <AppProviders>
        <SellWeightEntryDialog
          open
          product={product}
          stockHint={{ isTracked: true, onHandQuantity: 5 }}
          onConfirm={() => undefined}
          onCancel={() => undefined}
        />
      </AppProviders>,
    );

    await user.click(screen.getByTestId("sell-weight-unit-g"));
    await user.type(screen.getByTestId("sell-weight-input"), "250");
    expect(screen.getByTestId("sell-weight-conversion")).toHaveTextContent("250 g = 0.250 kg");
    expect(screen.getByTestId("sell-weight-preview")).toHaveTextContent(formatPeso(55));

    await user.clear(screen.getByTestId("sell-weight-input"));
    await user.type(screen.getByTestId("sell-weight-input"), "6000");
    expect(screen.getByTestId("sell-weight-inline-error")).toHaveTextContent(
      "Only 5.00 kg available.",
    );
    expect(screen.getByTestId("sell-weight-confirm")).toBeDisabled();
  });
});
