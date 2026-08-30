import { Button } from "@/components/ui/button";
import type { MessageKey } from "@/i18n/messages";

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
            {t("branches.mapFallback")}
          </p>
        </div>
      ) : (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-muted"
          data-testid="branch-map-provider-ready"
        >
          {t("branches.mapProviderReady")}
        </p>
      )}
      <div className="catalog-form-section__grid">
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
      <div className="flex flex-wrap gap-2">
        <Button
          type="button"
          variant="outline"
          className="min-h-11"
          disabled={gpsBusy || busy}
          onClick={onCaptureGps}
          data-testid="branch-gps-assist"
        >
          {gpsBusy ? t("branches.gpsWorking") : t("branches.gpsAssist")}
        </Button>
        {mapLinks ? (
          <>
            <Button asChild variant="outline" className="min-h-11">
              <a
                href={mapLinks.google}
                target="_blank"
                rel="noreferrer"
                data-testid="branch-maps-google"
              >
                {t("branches.openGoogleMaps")}
              </a>
            </Button>
            <Button asChild variant="outline" className="min-h-11">
              <a href={mapLinks.osm} target="_blank" rel="noreferrer" data-testid="branch-maps-osm">
                {t("branches.openOsm")}
              </a>
            </Button>
          </>
        ) : null}
      </div>
    </section>
  );
}
