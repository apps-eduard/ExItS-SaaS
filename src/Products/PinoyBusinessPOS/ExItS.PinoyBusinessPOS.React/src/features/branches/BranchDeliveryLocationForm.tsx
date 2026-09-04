import { lazy, Suspense, useState } from "react";
import { Button } from "@/components/ui/button";
import {
  formatCoordinateDisplay,
  isValidCoordinatePair,
  parseOptionalCoordinatePair,
} from "@/features/branches/branch-coordinates";
import type { MessageKey } from "@/i18n/messages";

const BranchMapPickerDialog = lazy(() =>
  import("@/features/branches/BranchMapPickerDialog").then((m) => ({
    default: m.BranchMapPickerDialog,
  })),
);

type MapLinks = { google: string; osm: string } | null;

type BranchDeliveryLocationFormProps = {
  latitude: string;
  longitude: string;
  mapProviderReady: boolean;
  mapLinks: MapLinks;
  gpsBusy: boolean;
  busy: boolean;
  t: (key: MessageKey) => string;
  onLatitudeChange: (value: string) => void;
  onLongitudeChange: (value: string) => void;
  onCaptureGps: () => void;
};

export function BranchDeliveryLocationForm({
  latitude,
  longitude,
  mapProviderReady,
  mapLinks,
  gpsBusy,
  busy,
  t,
  onLatitudeChange,
  onLongitudeChange,
  onCaptureGps,
}: BranchDeliveryLocationFormProps) {
  const [pickerOpen, setPickerOpen] = useState(false);
  const parsed = parseOptionalCoordinatePair(latitude, longitude);
  const selectedLat =
    parsed.ok && parsed.latitude != null && !parsed.clearCoordinates ? parsed.latitude : null;
  const selectedLng =
    parsed.ok && parsed.longitude != null && !parsed.clearCoordinates ? parsed.longitude : null;
  const hasSelection = isValidCoordinatePair(selectedLat, selectedLng);

  return (
    <section
      className="catalog-form-section exits-animate-panel gap-3"
      data-testid="branch-map-section"
    >
      <h2 className="catalog-form-section__title">{t("branches.mapTitle")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("branches.mapHint")}</p>

      {!mapProviderReady ? (
        <div className="exits-alert" data-testid="branch-map-fallback" role="status">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("branches.mapUnavailable")}
          </p>
        </div>
      ) : null}

      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          variant="outline"
          disabled={gpsBusy || busy}
          onClick={onCaptureGps}
          data-testid="branch-gps-assist"
        >
          {gpsBusy ? t("branches.gpsWorking") : t("branches.gpsAssist")}
        </Button>
        <Button
          type="button"
          variant="outline"
          disabled={busy || !mapProviderReady}
          onClick={() => setPickerOpen(true)}
          data-testid="branch-choose-on-map"
        >
          {hasSelection ? t("branches.mapChangeOnMap") : t("branches.mapChooseOnMap")}
        </Button>
      </div>

      <div className="flex flex-col gap-1.5" data-testid="branch-selected-location">
        <span className="text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.mapSelectedLocation")}
        </span>
        {hasSelection ? (
          <p className="m-0 font-mono text-[length:var(--exits-text-sm)]">
            {formatCoordinateDisplay(selectedLat)}, {formatCoordinateDisplay(selectedLng)}
          </p>
        ) : (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("branches.mapNoSelection")}
          </p>
        )}
      </div>

      {mapLinks ? (
        <div className="flex flex-wrap gap-2">
          <Button asChild variant="outline">
            <a
              href={mapLinks.google}
              target="_blank"
              rel="noreferrer"
              data-testid="branch-maps-google"
            >
              {t("branches.openGoogleMaps")}
            </a>
          </Button>
          <Button asChild variant="outline">
            <a href={mapLinks.osm} target="_blank" rel="noreferrer" data-testid="branch-maps-osm">
              {t("branches.openOsm")}
            </a>
          </Button>
        </div>
      ) : null}

      <details className="catalog-form-section__advanced" data-testid="branch-advanced-coordinates">
        <summary className="cursor-pointer text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.mapAdvancedCoordinates")}
        </summary>
        <div className="catalog-form-section__grid mt-3">
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.latitude")}
            <input
              type="number"
              step="any"
              className="catalog-form-select font-normal"
              value={latitude}
              onChange={(e) => onLatitudeChange(e.target.value)}
              data-testid="branch-latitude"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.longitude")}
            <input
              type="number"
              step="any"
              className="catalog-form-select font-normal"
              value={longitude}
              onChange={(e) => onLongitudeChange(e.target.value)}
              data-testid="branch-longitude"
            />
          </label>
        </div>
      </details>

      {pickerOpen ? (
        <Suspense fallback={null}>
          <BranchMapPickerDialog
            open={pickerOpen}
            initialLatitude={selectedLat}
            initialLongitude={selectedLng}
            t={t}
            onCancel={() => setPickerOpen(false)}
            onConfirm={(lat, lng) => {
              onLatitudeChange(String(lat));
              onLongitudeChange(String(lng));
              setPickerOpen(false);
            }}
          />
        </Suspense>
      ) : null}
    </section>
  );
}
