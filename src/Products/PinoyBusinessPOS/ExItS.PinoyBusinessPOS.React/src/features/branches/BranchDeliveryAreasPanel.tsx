import { useEffect, useId, useRef, useState, type KeyboardEvent } from "react";
import { X } from "lucide-react";
import type { BranchDeliveryServiceAreaDto } from "@/api/platform/branch-fulfillment-client";
import {
  searchPhilippineLocalities,
  type PhilippineLocalityDto,
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

export function BranchDeliveryAreasPanel({
  areas,
  busy,
  t,
  onAdd,
  onRemove,
  onReplace,
}: BranchDeliveryAreasPanelProps) {
  const listboxId = useId();
  const inputId = useId();
  const [query, setQuery] = useState("");
  const [results, setResults] = useState<PhilippineLocalityDto[]>([]);
  const [open, setOpen] = useState(false);
  const [highlight, setHighlight] = useState(-1);
  const [searching, setSearching] = useState(false);
  const [searchError, setSearchError] = useState<string | null>(null);
  const [replaceAreaId, setReplaceAreaId] = useState<string | null>(null);
  const inputRef = useRef<HTMLInputElement>(null);
  const debounceRef = useRef<number | null>(null);
  const abortRef = useRef<AbortController | null>(null);

  const selectedCodes = new Set(
    areas.filter((a) => a.psgcCode).map((a) => a.psgcCode as string),
  );

  useEffect(() => {
    if (debounceRef.current != null) {
      window.clearTimeout(debounceRef.current);
    }
    const trimmed = query.trim();
    if (trimmed.length < 2) {
      setResults([]);
      setSearching(false);
      setSearchError(null);
      setHighlight(-1);
      return;
    }

    debounceRef.current = window.setTimeout(() => {
      abortRef.current?.abort();
      const controller = new AbortController();
      abortRef.current = controller;
      setSearching(true);
      setSearchError(null);
      void searchPhilippineLocalities(trimmed, 20, controller.signal)
        .then((items) => {
          if (controller.signal.aborted) return;
          setResults(items);
          setOpen(true);
          setHighlight(items.length > 0 ? 0 : -1);
        })
        .catch((err: unknown) => {
          if (controller.signal.aborted) return;
          setResults([]);
          setSearchError(t("branches.deliveryAreas.searchFailed"));
          setHighlight(-1);
          void err;
        })
        .finally(() => {
          if (!controller.signal.aborted) {
            setSearching(false);
          }
        });
    }, 220);

    return () => {
      if (debounceRef.current != null) {
        window.clearTimeout(debounceRef.current);
      }
    };
  }, [query, t]);

  useEffect(
    () => () => {
      abortRef.current?.abort();
    },
    [],
  );

  async function selectLocality(locality: PhilippineLocalityDto) {
    if (busy || selectedCodes.has(locality.psgcCode)) {
      return;
    }
    setOpen(false);
    setQuery("");
    setResults([]);
    setHighlight(-1);
    if (replaceAreaId && onReplace) {
      const legacyId = replaceAreaId;
      setReplaceAreaId(null);
      await onReplace(legacyId, locality.psgcCode);
    } else {
      await onAdd(locality.psgcCode);
    }
    inputRef.current?.focus();
  }

  function onKeyDown(e: KeyboardEvent<HTMLInputElement>) {
    if (!open && (e.key === "ArrowDown" || e.key === "ArrowUp") && results.length > 0) {
      setOpen(true);
      return;
    }
    if (e.key === "Escape") {
      setOpen(false);
      setHighlight(-1);
      return;
    }
    if (e.key === "ArrowDown") {
      e.preventDefault();
      if (results.length === 0) return;
      setOpen(true);
      setHighlight((h) => (h + 1) % results.length);
      return;
    }
    if (e.key === "ArrowUp") {
      e.preventDefault();
      if (results.length === 0) return;
      setOpen(true);
      setHighlight((h) => (h <= 0 ? results.length - 1 : h - 1));
      return;
    }
    if (e.key === "Enter") {
      if (open && highlight >= 0 && highlight < results.length) {
        e.preventDefault();
        void selectLocality(results[highlight]!);
      }
    }
  }

  const showHint = query.trim().length < 2;

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

      <div className="branch-locality-search">
        <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold" htmlFor={inputId}>
          {t("branches.deliveryAreas.search")}
          <input
            id={inputId}
            ref={inputRef}
            className="catalog-form-select font-normal"
            value={query}
            onChange={(e) => {
              setQuery(e.target.value);
              setOpen(true);
            }}
            onKeyDown={onKeyDown}
            onFocus={() => {
              if (results.length > 0 || query.trim().length >= 2) {
                setOpen(true);
              }
            }}
            onBlur={() => {
              window.setTimeout(() => setOpen(false), 150);
            }}
            role="combobox"
            aria-expanded={open}
            aria-controls={listboxId}
            aria-autocomplete="list"
            aria-activedescendant={
              open && highlight >= 0 ? `${listboxId}-opt-${highlight}` : undefined
            }
            autoComplete="off"
            placeholder={t("branches.deliveryAreas.searchPlaceholder")}
            data-testid="delivery-area-search"
            disabled={busy}
          />
        </label>

        {replaceAreaId ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted" data-testid="delivery-area-replace-hint">
            {t("branches.deliveryAreas.replaceHint")}
          </p>
        ) : null}

        {open ? (
          <div className="branch-locality-results" data-testid="delivery-area-results">
            {showHint ? (
              <p className="branch-locality-results__hint m-0">{t("branches.deliveryAreas.typeToSearch")}</p>
            ) : searching ? (
              <p className="branch-locality-results__hint m-0">{t("branches.deliveryAreas.searching")}</p>
            ) : searchError ? (
              <p className="branch-locality-results__hint m-0" role="alert">
                {searchError}
              </p>
            ) : results.length === 0 ? (
              <p className="branch-locality-results__hint m-0">{t("branches.deliveryAreas.noMatch")}</p>
            ) : (
              <ul
                id={listboxId}
                role="listbox"
                className="branch-locality-results__list m-0 list-none p-0"
              >
                {results.map((item, index) => {
                  const already = selectedCodes.has(item.psgcCode);
                  const typeLabel = localityTypeLabel(item.localityType, t);
                  const geo = item.provinceName ?? item.regionName;
                  const secondary = [typeLabel, geo].filter(Boolean).join(" · ");
                  return (
                    <li key={item.psgcCode} role="presentation">
                      <button
                        type="button"
                        id={`${listboxId}-opt-${index}`}
                        role="option"
                        aria-selected={highlight === index}
                        aria-disabled={already || undefined}
                        disabled={already || busy}
                        className={
                          highlight === index
                            ? "branch-locality-results__option is-active"
                            : "branch-locality-results__option"
                        }
                        data-testid={`delivery-area-result-${item.psgcCode}`}
                        onMouseDown={(e) => e.preventDefault()}
                        onClick={() => void selectLocality(item)}
                      >
                        <span className="branch-locality-results__name">
                          {friendlyName(item.name)}
                        </span>
                        <span className="branch-locality-results__meta">{secondary}</span>
                        <span className="branch-locality-results__region">{item.regionName}</span>
                        {already ? (
                          <span className="branch-locality-results__badge">
                            {t("branches.deliveryAreas.alreadyAdded")}
                          </span>
                        ) : null}
                      </button>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>
        ) : null}
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
                      disabled={busy}
                      data-testid={`replace-delivery-area-${area.id}`}
                      onClick={() => {
                        setReplaceAreaId(area.id);
                        inputRef.current?.focus();
                        setOpen(true);
                      }}
                    >
                      {t("branches.deliveryAreas.replace")}
                    </button>
                  ) : null}
                  <button
                    type="button"
                    className="branch-area-chip__remove"
                    disabled={busy}
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
