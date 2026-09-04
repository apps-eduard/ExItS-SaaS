import { useEffect, useMemo, useState } from "react";
import { MapContainer, Marker, TileLayer, useMapEvents } from "react-leaflet";
import L from "leaflet";
import markerIcon2x from "leaflet/dist/images/marker-icon-2x.png";
import markerIcon from "leaflet/dist/images/marker-icon.png";
import markerShadow from "leaflet/dist/images/marker-shadow.png";
import "leaflet/dist/leaflet.css";
import { createPortal } from "react-dom";
import { Button } from "@/components/ui/button";
import { formatCoordinateDisplay } from "@/features/branches/branch-coordinates";
import {
  DEFAULT_MAP_ZOOM,
  PHILIPPINES_DEFAULT_CENTER,
  resolveMapTilesAttribution,
  resolveMapTilesUrl,
} from "@/features/branches/branch-map-config";
import { cn } from "@/lib/cn";
import type { MessageKey } from "@/i18n/messages";

// Leaflet's default icon URLs break under Vite bundling without an explicit fix.
L.Icon.Default.mergeOptions({
  iconRetinaUrl: markerIcon2x,
  iconUrl: markerIcon,
  shadowUrl: markerShadow,
});

export type BranchMapPickerDialogProps = {
  open: boolean;
  initialLatitude: number | null;
  initialLongitude: number | null;
  t: (key: MessageKey) => string;
  onCancel: () => void;
  onConfirm: (latitude: number, longitude: number) => void;
};

type DraftPoint = { latitude: number; longitude: number };

function MapClickHandler({ onPick }: { onPick: (lat: number, lng: number) => void }) {
  useMapEvents({
    click(event) {
      onPick(event.latlng.lat, event.latlng.lng);
    },
  });
  return null;
}

/**
 * Responsive accessible map picker (dialog on desktop, full-width sheet-like on mobile).
 * Confirm updates the parent draft only — does not persist.
 */
export function BranchMapPickerDialog({
  open,
  initialLatitude,
  initialLongitude,
  t,
  onCancel,
  onConfirm,
}: BranchMapPickerDialogProps) {
  const tilesUrl = resolveMapTilesUrl();
  const attribution = resolveMapTilesAttribution();

  const start = useMemo<DraftPoint>(() => {
    if (
      initialLatitude != null &&
      initialLongitude != null &&
      Number.isFinite(initialLatitude) &&
      Number.isFinite(initialLongitude)
    ) {
      return { latitude: initialLatitude, longitude: initialLongitude };
    }
    return {
      latitude: PHILIPPINES_DEFAULT_CENTER.latitude,
      longitude: PHILIPPINES_DEFAULT_CENTER.longitude,
    };
  }, [initialLatitude, initialLongitude]);

  const [draft, setDraft] = useState<DraftPoint>(start);
  const hasInitialPair =
    initialLatitude != null &&
    initialLongitude != null &&
    Number.isFinite(initialLatitude) &&
    Number.isFinite(initialLongitude);

  useEffect(() => {
    if (!open) {
      return;
    }
    setDraft(start);
  }, [open, start]);

  if (!open || typeof document === "undefined") {
    return null;
  }

  return createPortal(
    <>
      <div
        className="fixed inset-0 z-40 bg-black/40"
        role="presentation"
        onClick={onCancel}
        data-testid="branch-map-picker-backdrop"
      />
      <div
        className={cn(
          "fixed inset-x-0 bottom-0 z-50 flex max-h-[92dvh] flex-col gap-3 rounded-t-[var(--exits-radius-lg)] border border-border bg-surface p-4 shadow-[0_-8px_32px_rgba(0,0,0,0.12)]",
          "sm:inset-auto sm:left-1/2 sm:top-1/2 sm:w-[min(560px,calc(100vw-2rem))] sm:-translate-x-1/2 sm:-translate-y-1/2 sm:rounded-[var(--exits-radius-lg)] sm:max-h-[min(720px,90dvh)]",
        )}
        role="dialog"
        aria-modal="true"
        aria-labelledby="branch-map-picker-title"
        data-testid="branch-map-picker"
      >
        <h2
          id="branch-map-picker-title"
          className="m-0 text-[length:var(--exits-text-md)] font-semibold"
        >
          {t("branches.mapPickerTitle")}
        </h2>

        {!tilesUrl ? (
          <div className="exits-alert" role="status" data-testid="branch-map-picker-unavailable">
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("branches.mapUnavailable")}
            </p>
          </div>
        ) : (
          <div
            className="branch-map-picker__map min-h-[240px] flex-1 overflow-hidden rounded-[var(--exits-radius-md)] border border-border"
            data-testid="branch-map-picker-canvas"
          >
            <MapContainer
              center={[draft.latitude, draft.longitude]}
              zoom={hasInitialPair ? DEFAULT_MAP_ZOOM : 6}
              className="h-[min(48dvh,360px)] w-full sm:h-[360px]"
              scrollWheelZoom
            >
              <TileLayer attribution={attribution} url={tilesUrl} />
              <MapClickHandler
                onPick={(lat, lng) => setDraft({ latitude: lat, longitude: lng })}
              />
              <Marker
                position={[draft.latitude, draft.longitude]}
                draggable
                eventHandlers={{
                  dragend: (event) => {
                    const marker = event.target as L.Marker;
                    const pos = marker.getLatLng();
                    setDraft({ latitude: pos.lat, longitude: pos.lng });
                  },
                }}
              />
            </MapContainer>
          </div>
        )}

        <p className="m-0 font-mono text-[length:var(--exits-text-sm)]" data-testid="branch-map-picker-coords">
          {formatCoordinateDisplay(draft.latitude)}, {formatCoordinateDisplay(draft.longitude)}
        </p>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("branches.mapPickerHint")}</p>

        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="outline"
            onClick={onCancel}
            data-testid="branch-map-picker-cancel"
          >
            {t("branches.cancel")}
          </Button>
          <Button
            type="button"
            disabled={!tilesUrl}
            onClick={() => onConfirm(draft.latitude, draft.longitude)}
            data-testid="branch-map-picker-confirm"
          >
            {t("branches.mapUseThisLocation")}
          </Button>
        </div>
      </div>
    </>,
    document.body,
  );
}
