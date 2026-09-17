import { useEffect, useId, useRef, useState } from "react";
import { X } from "lucide-react";
import type { BranchDeliveryServiceAreaDto } from "@/api/platform/branch-fulfillment-client";
import {
  listPhilippineLocalitiesByRegion,
  listPhilippineRegions,
  type PhilippineLocalityDto,
  type PhilippineRegionDto,
} from "@/api/platform/ph-locality-client";
import type { MessageKey } from "@/i18n/messages";

type BranchDeliveryAreasPanelProps = {
  areas: BranchDeliveryServiceAreaDto[];
  busy: boolean;
  t: (key: MessageKey) => string;
  onAdd: (psgcCode: string) => Promise<void>;
  onRemove: (areaId: string) => Promise<void>;
  onReplace?: (areaId: string, psgcCode: string) => Promise<void>;
};

function friendlyName(name: string): string {
  if (name.toLowerCase().startsWith("city of ")) {
    const rest = name.slice("city of ".length).trim();
    if (rest && !rest.toLowerCase().endsWith(" city")) {
      return `${rest} City`;
    }
  }
  return name;
}

function localityTypeLabel(
  localityType: string | null | undefined,
  t: (key: MessageKey) => string,
): string {
  if (localityType === "Municipality") {
    return t("branches.deliveryAreas.municipality");
  }
  if (localityType === "City") {
    return t("branches.deliveryAreas.cityType");
  }
  return localityType ?? "";
}

function matchesCityFilter(item: PhilippineLocalityDto, query: string): boolean {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  const haystack = [
    item.name,
    friendlyName(item.name),
    item.displayLabel,
    item.provinceName ?? "",
    item.localityType,
  ]
    .join(" ")
    .toLowerCase();
  return haystack.includes(q);
}

function matchesRegionFilter(region: PhilippineRegionDto, query: string): boolean {
  const q = query.trim().toLowerCase();
  if (!q) return true;
  return region.regionName.toLowerCase().includes(q) || region.regionCode.includes(q);
}

export function BranchDeliveryAreasPanel({
  areas,
  busy,
  t,
  onAdd,
  onRemove,
  onReplace,
}: BranchDeliveryAreasPanelProps) {
  const regionSearchId = useId();
  const citySearchId = useId();
  const cityListId = useId();
  const [regions, setRegions] = useState<PhilippineRegionDto[]>([]);
  const [regionsLoading, setRegionsLoading] = useState(true);
  const [regionsError, setRegionsError] = useState<string | null>(null);
  const [regionCode, setRegionCode] = useState("");
  const [regionFilter, setRegionFilter] = useState("");
  const [regionPickerOpen, setRegionPickerOpen] = useState(true);
  const [cities, setCities] = useState<PhilippineLocalityDto[]>([]);
  const [citiesLoading, setCitiesLoading] = useState(false);
  const [citiesError, setCitiesError] = useState<string | null>(null);
  const [cityFilter, setCityFilter] = useState("");
  const [cityPickerOpen, setCityPickerOpen] = useState(false);
  const [replaceAreaId, setReplaceAreaId] = useState<string | null>(null);
  const [pendingCodes, setPendingCodes] = useState<string[]>([]);
  const [regionsReloadKey, setRegionsReloadKey] = useState(0);
  const regionsAbortRef = useRef<AbortController | null>(null);
  const citiesAbortRef = useRef<AbortController | null>(null);
  const cityPanelRef = useRef<HTMLDivElement>(null);
  const cityBlurTimerRef = useRef<number | null>(null);

  const selectedByCode = new Map<string, BranchDeliveryServiceAreaDto>();
  for (const area of areas) {
    if (area.psgcCode) {
      selectedByCode.set(area.psgcCode, area);
    }
  }

  const selectedCodes = new Set([...selectedByCode.keys(), ...pendingCodes]);
  const filteredRegions = regions.filter((region) => matchesRegionFilter(region, regionFilter));
  const filteredCities = cities.filter((item) => matchesCityFilter(item, cityFilter));
  const selectedRegion = regions.find((r) => r.regionCode === regionCode) ?? null;

  useEffect(() => {
    setPendingCodes((prev) => {
      const stillPending = prev.filter(
        (code) => !areas.some((area) => area.psgcCode === code),
      );
      return stillPending.length === prev.length ? prev : stillPending;
    });
  }, [areas]);

  useEffect(() => {
    regionsAbortRef.current?.abort();
    const controller = new AbortController();
    regionsAbortRef.current = controller;
    setRegionsLoading(true);
    setRegionsError(null);
    void listPhilippineRegions(controller.signal)
      .then((items) => {
        if (controller.signal.aborted) return;
        setRegions(items);
      })
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        setRegions([]);
        setRegionsError(t("branches.deliveryAreas.regionsFailed"));
        void err;
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setRegionsLoading(false);
        }
      });

    return () => {
      controller.abort();
    };
  }, [t, regionsReloadKey]);

  useEffect(() => {
    citiesAbortRef.current?.abort();
    setCityFilter("");
    setCityPickerOpen(false);
    if (!regionCode) {
      setCities([]);
      setCitiesLoading(false);
      setCitiesError(null);
      return;
    }

    const controller = new AbortController();
    citiesAbortRef.current = controller;
    setCitiesLoading(true);
    setCitiesError(null);
    void listPhilippineLocalitiesByRegion(regionCode, controller.signal)
      .then((items) => {
        if (controller.signal.aborted) return;
        setCities(items);
        setCityPickerOpen(true);
      })
      .catch((err: unknown) => {
        if (controller.signal.aborted) return;
        setCities([]);
        setCitiesError(t("branches.deliveryAreas.citiesFailed"));
        void err;
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setCitiesLoading(false);
        }
      });

    return () => {
      controller.abort();
    };
  }, [regionCode, t]);

  useEffect(
    () => () => {
      regionsAbortRef.current?.abort();
      citiesAbortRef.current?.abort();
      if (cityBlurTimerRef.current != null) {
        window.clearTimeout(cityBlurTimerRef.current);
      }
    },
    [],
  );

  function closeCityPicker() {
    setCityPickerOpen(false);
    setCityFilter("");
  }

  function openCityPicker() {
    if (!regionCode || citiesLoading || Boolean(citiesError) || cities.length === 0) return;
    setCityPickerOpen(true);
  }

  async function toggleCity(locality: PhilippineLocalityDto, nextChecked: boolean) {
    if (replaceAreaId && onReplace) {
      if (busy || !nextChecked || selectedCodes.has(locality.psgcCode)) return;
      const legacyId = replaceAreaId;
      setReplaceAreaId(null);
      closeCityPicker();
      await onReplace(legacyId, locality.psgcCode);
      return;
    }

    if (nextChecked) {
      if (selectedCodes.has(locality.psgcCode)) return;
      setPendingCodes((prev) =>
        prev.includes(locality.psgcCode) ? prev : [...prev, locality.psgcCode],
      );
      closeCityPicker();
      try {
        await onAdd(locality.psgcCode);
      } catch {
        setPendingCodes((prev) => prev.filter((code) => code !== locality.psgcCode));
      }
      return;
    }

    const existing = selectedByCode.get(locality.psgcCode);
    if (!existing) {
      setPendingCodes((prev) => prev.filter((code) => code !== locality.psgcCode));
      return;
    }
    await onRemove(existing.id);
  }

  const controlsBusy = busy;

  return (
    <section
      className="catalog-form-section exits-animate-panel gap-3"
      data-testid="branch-delivery-areas"
    >
      <h2 className="catalog-form-section__title">{t("branches.deliveryAreasTitle")}</h2>
      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("branches.deliveryAreasLede")}
      </p>

      <div
        className="branch-delivery-country"
        data-testid="delivery-area-country-readonly"
      >
        <span className="branch-delivery-country__label">{t("branches.deliveryAreas.country")}</span>
        <span className="branch-delivery-country__value">
          {t("branches.deliveryAreas.philippines")}
        </span>
      </div>

      <div className="branch-locality-picker">
        <div className="branch-locality-region-panel" data-testid="delivery-area-region-panel">
          <span className="text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.deliveryAreas.regionSelect")}
          </span>

          {selectedRegion && !regionPickerOpen ? (
            <div className="branch-locality-region-chosen" data-testid="delivery-area-region-selected">
              <span className="branch-locality-region-chosen__name">{selectedRegion.regionName}</span>
              <button
                type="button"
                className="branch-locality-region-chosen__change"
                data-testid="delivery-area-region-change"
                onClick={() => {
                  setRegionFilter("");
                  setRegionPickerOpen(true);
                }}
              >
                {t("branches.deliveryAreas.changeRegion")}
              </button>
            </div>
          ) : (
            <>
              <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold" htmlFor={regionSearchId}>
                <span className="sr-only">{t("branches.deliveryAreas.regionSearchPlaceholder")}</span>
                <input
                  id={regionSearchId}
                  type="search"
                  className="exits-input font-normal"
                  value={regionFilter}
                  disabled={regionsLoading || Boolean(regionsError)}
                  onChange={(e) => setRegionFilter(e.target.value)}
                  placeholder={
                    regionsLoading
                      ? t("branches.deliveryAreas.regionsLoading")
                      : t("branches.deliveryAreas.regionSearchPlaceholder")
                  }
                  data-testid="delivery-area-region-search"
                  autoComplete="off"
                />
              </label>

              {regionsLoading ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("branches.deliveryAreas.regionsLoading")}
                </p>
              ) : regionsError ? null : regions.length === 0 ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("branches.deliveryAreas.regionsFailed")}
                </p>
              ) : filteredRegions.length === 0 ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="delivery-area-region-no-match">
                  {t("branches.deliveryAreas.noRegionMatch")}
                </p>
              ) : (
                <ul
                  className="branch-locality-region-list m-0 list-none p-0"
                  role="listbox"
                  aria-label={t("branches.deliveryAreas.regionSelect")}
                  data-testid="delivery-area-region-list"
                >
                  {filteredRegions.map((region) => {
                    const selected = region.regionCode === regionCode;
                    return (
                      <li key={region.regionCode} role="presentation">
                        <button
                          type="button"
                          role="option"
                          aria-selected={selected}
                          className={
                            selected
                              ? "branch-locality-region-option is-selected"
                              : "branch-locality-region-option"
                          }
                          data-testid={`delivery-area-region-${region.regionCode}`}
                          onClick={() => {
                            setRegionCode(region.regionCode);
                            setRegionFilter("");
                            setRegionPickerOpen(false);
                          }}
                        >
                          {region.regionName}
                        </button>
                      </li>
                    );
                  })}
                </ul>
              )}
            </>
          )}
        </div>

        {regionsError ? (
          <p className="m-0 flex flex-wrap items-center gap-2 text-[length:var(--exits-text-sm)] text-muted" role="alert">
            <span>{regionsError}</span>
            <button
              type="button"
              className="font-semibold text-[var(--exits-primary)] underline"
              data-testid="delivery-area-regions-retry"
              onClick={() => setRegionsReloadKey((k) => k + 1)}
            >
              {t("orders.retry")}
            </button>
          </p>
        ) : null}

        {replaceAreaId ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="delivery-area-replace-hint">
            {t("branches.deliveryAreas.replaceHint")}
          </p>
        ) : null}

        {!regionCode ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="delivery-area-pick-region">
            {t("branches.deliveryAreas.pickRegionFirst")}
          </p>
        ) : (
          <div
            className="branch-locality-city-panel"
            data-testid="delivery-area-city-panel"
            ref={cityPanelRef}
            onBlur={(e) => {
              const next = e.relatedTarget as Node | null;
              if (next && cityPanelRef.current?.contains(next)) return;
              if (cityBlurTimerRef.current != null) {
                window.clearTimeout(cityBlurTimerRef.current);
              }
              cityBlurTimerRef.current = window.setTimeout(() => {
                setCityPickerOpen(false);
              }, 120);
            }}
            onFocus={() => {
              if (cityBlurTimerRef.current != null) {
                window.clearTimeout(cityBlurTimerRef.current);
                cityBlurTimerRef.current = null;
              }
            }}
          >
            <label
              className="flex min-w-0 flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold"
              htmlFor={citySearchId}
            >
              {t("branches.deliveryAreas.citySelect")}
              <input
                id={citySearchId}
                type="search"
                className="exits-input font-normal"
                value={cityFilter}
                disabled={citiesLoading || Boolean(citiesError)}
                onChange={(e) => {
                  setCityFilter(e.target.value);
                  openCityPicker();
                }}
                onFocus={openCityPicker}
                onClick={openCityPicker}
                onKeyDown={(e) => {
                  if (e.key === "Escape") {
                    e.preventDefault();
                    closeCityPicker();
                  }
                }}
                placeholder={t("branches.deliveryAreas.searchPlaceholder")}
                data-testid="delivery-area-city-search"
                autoComplete="off"
                aria-expanded={cityPickerOpen}
                aria-controls={cityListId}
              />
            </label>

            {citiesLoading ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("branches.deliveryAreas.citiesLoading")}
              </p>
            ) : citiesError ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" role="alert">
                {citiesError}
              </p>
            ) : cities.length === 0 ? (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("branches.deliveryAreas.noCitiesInRegion")}
              </p>
            ) : cityPickerOpen ? (
              filteredCities.length === 0 ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="delivery-area-city-no-match">
                  {t("branches.deliveryAreas.noMatch")}
                </p>
              ) : (
                <ul
                  id={cityListId}
                  className="branch-locality-city-list m-0 list-none p-0"
                  role="listbox"
                  aria-label={t("branches.deliveryAreas.citiesInRegion")}
                  data-testid="delivery-area-city-list"
                >
                  {filteredCities.map((item) => {
                    const checked = selectedCodes.has(item.psgcCode);
                    const typeLabel = localityTypeLabel(item.localityType, t);
                    const geo = item.provinceName;
                    const secondary = [typeLabel, geo].filter(Boolean).join(" · ");
                    const replaceBlocked = Boolean(replaceAreaId) && checked;
                    return (
                      <li key={item.psgcCode} role="presentation">
                        <label
                          className={
                            checked
                              ? "branch-locality-city-option is-selected catalog-form-check"
                              : "branch-locality-city-option catalog-form-check"
                          }
                          data-testid={`delivery-area-city-${item.psgcCode}`}
                        >
                          <input
                            type="checkbox"
                            checked={checked}
                            disabled={controlsBusy || replaceBlocked}
                            onMouseDown={(e) => {
                              // Keep focus in panel so blur does not close before click.
                              e.preventDefault();
                            }}
                            onChange={(e) => void toggleCity(item, e.target.checked)}
                          />
                          <span className="branch-locality-city-option__text">
                            <span className="branch-locality-city-option__name">
                              {friendlyName(item.name)}
                            </span>
                            {secondary ? (
                              <span className="branch-locality-city-option__meta">{secondary}</span>
                            ) : null}
                          </span>
                        </label>
                      </li>
                    );
                  })}
                </ul>
              )
            ) : null}
          </div>
        )}
      </div>

      <div>
        <h3 className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
          {t("branches.deliveryAreas.selectedTitle")}
        </h3>
        {areas.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="delivery-areas-empty">
            {t("branches.deliveryAreasEmpty")}
          </p>
        ) : (
          <ul className="branch-area-chips m-0 flex list-none flex-wrap gap-2 p-0" data-testid="delivery-areas-list">
            {areas.map((area) => {
              const label = friendlyName(area.displayLabel || area.cityMunicipalityName);
              const secondary = area.provinceName ?? area.regionName ?? area.regionOrProvinceName;
              const chipTitle = [label, secondary].filter(Boolean).join(" · ");
              const unverified = !area.isVerified;
              return (
                <li
                  key={area.id}
                  className={unverified ? "branch-area-chip is-unverified" : "branch-area-chip"}
                  title={chipTitle}
                >
                  <span className="branch-area-chip__stack">
                    <span className="branch-area-chip__label">{label}</span>
                    {secondary ? (
                      <span className="branch-area-chip__secondary">{secondary}</span>
                    ) : null}
                    {unverified ? (
                      <span className="branch-area-chip__badge" data-testid={`delivery-area-unverified-${area.id}`}>
                        {t("branches.deliveryAreas.needsVerification")}
                      </span>
                    ) : null}
                  </span>
                  {unverified && onReplace ? (
                    <button
                      type="button"
                      className="branch-area-chip__replace"
                      disabled={controlsBusy}
                      data-testid={`replace-delivery-area-${area.id}`}
                      onClick={() => setReplaceAreaId(area.id)}
                    >
                      {t("branches.deliveryAreas.replace")}
                    </button>
                  ) : null}
                  <button
                    type="button"
                    className="branch-area-chip__remove"
                    disabled={controlsBusy}
                    aria-label={`${t("branches.deliveryAreas.remove")}: ${chipTitle}`}
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
      </div>
    </section>
  );
}
