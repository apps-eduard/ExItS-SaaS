import { useState } from "react";
import { X } from "lucide-react";
import type { BranchDeliveryServiceAreaDto } from "@/api/platform/branch-fulfillment-client";
import { Button } from "@/components/ui/button";
import type { MessageKey } from "@/i18n/messages";

type BranchDeliveryAreasPanelProps = {
  areas: BranchDeliveryServiceAreaDto[];
  busy: boolean;
  t: (key: MessageKey) => string;
  onAdd: (input: {
    countryCode: string;
    cityMunicipalityName: string;
    regionOrProvinceName: string | null;
  }) => Promise<void>;
  onRemove: (areaId: string) => Promise<void>;
};

export function BranchDeliveryAreasPanel({
  areas,
  busy,
  t,
  onAdd,
  onRemove,
}: BranchDeliveryAreasPanelProps) {
  const [city, setCity] = useState("");
  const [region, setRegion] = useState("");
  const [countryCode, setCountryCode] = useState("PH");
  const [localError, setLocalError] = useState<string | null>(null);

  async function handleAdd() {
    const trimmedCity = city.trim();
    if (!trimmedCity) {
      setLocalError(t("branches.deliveryAreas.cityRequired"));
      return;
    }
    setLocalError(null);
    await onAdd({
      countryCode: countryCode.trim() || "PH",
      cityMunicipalityName: trimmedCity,
      regionOrProvinceName: region.trim() || null,
    });
    setCity("");
    setRegion("");
  }

  return (
    <section
      className="catalog-form-section exits-animate-panel gap-3"
      data-testid="branch-delivery-areas"
    >
      <h2 className="catalog-form-section__title">{t("branches.deliveryAreasTitle")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("branches.deliveryAreasLede")}
      </p>

      {areas.length === 0 ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="delivery-areas-empty">
          {t("branches.deliveryAreasEmpty")}
        </p>
      ) : (
        <ul className="branch-area-chips m-0 flex list-none flex-wrap gap-2 p-0" data-testid="delivery-areas-list">
          {areas.map((area) => {
            const label = [area.cityMunicipalityName, area.regionOrProvinceName]
              .filter(Boolean)
              .join(", ");
            return (
              <li key={area.id} className="branch-area-chip">
                <span className="branch-area-chip__label">{label}</span>
                <button
                  type="button"
                  className="branch-area-chip__remove"
                  disabled={busy}
                  aria-label={t("branches.deliveryAreas.remove")}
                  data-testid={`remove-delivery-area-${area.id}`}
                  onClick={() => void onRemove(area.id)}
                >
                  <X className="size-3.5" aria-hidden />
                </button>
              </li>
            );
          })}
        </ul>
      )}

      <div className="catalog-form-section__grid">
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.deliveryAreas.city")}
          <input
            className="catalog-form-select font-normal"
            value={city}
            onChange={(e) => setCity(e.target.value)}
            data-testid="delivery-area-city"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.deliveryAreas.region")}
          <input
            className="catalog-form-select font-normal"
            value={region}
            onChange={(e) => setRegion(e.target.value)}
            data-testid="delivery-area-region"
          />
        </label>
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.countryCode")}
          <input
            className="catalog-form-select font-normal"
            value={countryCode}
            onChange={(e) => setCountryCode(e.target.value)}
            data-testid="delivery-area-country"
          />
        </label>
      </div>

      {localError ? (
        <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive" role="alert">
          {localError}
        </p>
      ) : null}

      <Button
        type="button"
        className="min-h-11 w-fit"
        disabled={busy}
        onClick={() => void handleAdd()}
        data-testid="add-delivery-area"
      >
        {t("branches.deliveryAreas.add")}
      </Button>
    </section>
  );
}
