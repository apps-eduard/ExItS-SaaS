import { describe, expect, it, vi } from "vitest";
import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { BranchDeliveryLocationForm } from "@/features/branches/BranchDeliveryLocationForm";

vi.mock("@/features/branches/BranchMapPickerDialog", () => ({
  BranchMapPickerDialog: ({
    open,
    onConfirm,
    onCancel,
  }: {
    open: boolean;
    onConfirm: (lat: number, lng: number) => void;
    onCancel: () => void;
  }) =>
    open ? (
      <div data-testid="branch-map-picker">
        <button
          type="button"
          data-testid="branch-map-picker-confirm"
          onClick={() => onConfirm(10.7, 122.96)}
        >
          confirm
        </button>
        <button type="button" data-testid="branch-map-picker-cancel" onClick={onCancel}>
          cancel
        </button>
      </div>
    ) : null,
}));

describe("BranchDeliveryLocationForm map UX", () => {
  it("opens picker and applies confirmed coordinates without auto-save", async () => {
    const user = userEvent.setup();
    const onLatitudeChange = vi.fn();
    const onLongitudeChange = vi.fn();
    render(
      <BranchDeliveryLocationForm
        latitude="10.6765"
        longitude="122.9509"
        mapProviderReady
        mapLinks={{ google: "https://maps.example/g", osm: "https://maps.example/o" }}
        gpsBusy={false}
        busy={false}
        t={(key) => key}
        onLatitudeChange={onLatitudeChange}
        onLongitudeChange={onLongitudeChange}
        onCaptureGps={vi.fn()}
      />,
    );

    expect(screen.getByTestId("branch-choose-on-map")).toBeInTheDocument();
    expect(screen.getByTestId("branch-gps-assist")).toBeInTheDocument();
    expect(screen.getByTestId("branch-maps-google")).toHaveAttribute(
      "href",
      "https://maps.example/g",
    );

    await user.click(screen.getByTestId("branch-choose-on-map"));
    expect(await screen.findByTestId("branch-map-picker")).toBeInTheDocument();
    await user.click(screen.getByTestId("branch-map-picker-confirm"));
    expect(onLatitudeChange).toHaveBeenCalledWith("10.7");
    expect(onLongitudeChange).toHaveBeenCalledWith("122.96");
  });

  it("shows map unavailable fallback when provider is not ready", () => {
    render(
      <BranchDeliveryLocationForm
        latitude=""
        longitude=""
        mapProviderReady={false}
        mapLinks={null}
        gpsBusy={false}
        busy={false}
        t={(key) => key}
        onLatitudeChange={vi.fn()}
        onLongitudeChange={vi.fn()}
        onCaptureGps={vi.fn()}
      />,
    );
    expect(screen.getByTestId("branch-map-fallback")).toHaveTextContent(
      "branches.mapUnavailable",
    );
    expect(screen.getByTestId("branch-choose-on-map")).toBeDisabled();
  });
});
