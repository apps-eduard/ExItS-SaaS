import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { CircleAlert, CircleCheck, Loader2, Save } from "lucide-react";
import { canManageBranchFulfillment } from "@/access/pos-capabilities";
import {
  getBranchFulfillmentReadiness,
  getBranchOperatingHours,
  listOrganizationBranchesForFulfillment,
  setBranchOnlineOrdersPaused,
  updateBranchFulfillmentSettings,
  updateOrganizationBranch,
  upsertBranchDeliveryPolicy,
  upsertBranchOperatingHours,
  type BranchFulfillmentReadinessDto,
  type OrganizationBranchDto,
} from "@/api/platform/branch-fulfillment-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { StatusChip } from "@/components/exits/StatusChip";
import {
  formatCoordinate,
  isMapProviderConfigured,
  parseOptionalCoordinatePair,
} from "@/features/branches/branch-coordinates";
import {
  defaultHoursSchedule,
  hasConfiguredHours,
  hoursFromDto,
  hoursToRequest,
  ORDERED_WEEKDAYS,
  type HoursDayDraft,
} from "@/features/branches/branch-hours";
import { externalMapLinks, requestGpsAssistOnce } from "@/features/branches/branch-map-links";
import {
  deliveryEnablementLabel,
  filterRedundantReasonCodes,
  missingRequirementMessageKey,
  orderingEnablementLabel,
  pickupEnablementLabel,
  reasonCodeMessageKey,
  type EnablementLabel,
} from "@/features/branches/branch-readiness-labels";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function enablementTone(label: EnablementLabel): "success" | "warning" | "info" | "danger" {
  if (label === "enabled") return "success";
  if (label === "paused" || label === "notReady") return "warning";
  return "info";
}

function enablementStatusWord(label: EnablementLabel): MessageKey {
  if (label === "enabled") return "branches.status.enabled";
  if (label === "paused") return "branches.status.paused";
  if (label === "notReady") return "branches.status.notReady";
  return "branches.status.disabled";
}

function enablementChannelKey(kind: "ordering" | "pickup" | "delivery"): MessageKey {
  if (kind === "ordering") return "branches.channel.ordering";
  if (kind === "pickup") return "branches.channel.pickup";
  return "branches.channel.delivery";
}

function dayLabelKey(day: string): MessageKey {
  const map: Record<string, MessageKey> = {
    Monday: "branches.day.monday",
    Tuesday: "branches.day.tuesday",
    Wednesday: "branches.day.wednesday",
    Thursday: "branches.day.thursday",
    Friday: "branches.day.friday",
    Saturday: "branches.day.saturday",
    Sunday: "branches.day.sunday",
  };
  return map[day] ?? "branches.day.monday";
}

export function BranchFulfillmentEditPage() {
  const { t } = useI18n();
  const { branchId = "" } = useParams();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManageBranchFulfillment(sessionGrant);
  const organizationId = boundWorkspace?.organizationId;
  const mapProviderReady = isMapProviderConfigured();

  const detailQuery = useQuery({
    queryKey: ["branch-fulfillment-detail", organizationId, branchId],
    enabled: Boolean(organizationId && branchId && canManage),
    queryFn: async ({ signal }) => {
      const [branches, readiness, hours] = await Promise.all([
        listOrganizationBranchesForFulfillment(organizationId!, signal),
        getBranchFulfillmentReadiness(organizationId!, branchId, signal),
        getBranchOperatingHours(organizationId!, branchId, signal),
      ]);
      const branch = branches.find((b) => b.id === branchId) ?? null;
      return { branch, readiness, hours };
    },
  });

  const [name, setName] = useState("");
  const [contactPhone, setContactPhone] = useState("");
  const [timeZoneId, setTimeZoneId] = useState("");
  const [addressLine1, setAddressLine1] = useState("");
  const [addressLine2, setAddressLine2] = useState("");
  const [city, setCity] = useState("");
  const [region, setRegion] = useState("");
  const [postalCode, setPostalCode] = useState("");
  const [countryCode, setCountryCode] = useState("PH");
  const [latitude, setLatitude] = useState("");
  const [longitude, setLongitude] = useState("");
  const [hours, setHours] = useState<HoursDayDraft[]>(defaultHoursSchedule);
  const [minimumOrder, setMinimumOrder] = useState("0");
  const [baseFee, setBaseFee] = useState("0");
  const [includedKm, setIncludedKm] = useState("0");
  const [additionalPerKm, setAdditionalPerKm] = useState("0");
  const [maximumKm, setMaximumKm] = useState("0");
  const [freeThreshold, setFreeThreshold] = useState("");
  const [readiness, setReadiness] = useState<BranchFulfillmentReadinessDto | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [okMessage, setOkMessage] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);
  const [gpsBusy, setGpsBusy] = useState(false);
  const [hydrated, setHydrated] = useState(false);

  useEffect(() => {
    const data = detailQuery.data;
    if (!data?.branch || hydrated) {
      return;
    }
    applyBranch(data.branch);
    setReadiness(data.readiness);
    setHours(hoursFromDto(data.hours));
    setHydrated(true);
  }, [detailQuery.data, hydrated]);

  useEffect(() => {
    setHydrated(false);
  }, [branchId, organizationId]);

  function applyBranch(branch: OrganizationBranchDto) {
    setName(branch.name);
    setContactPhone(branch.contactPhone ?? "");
    setTimeZoneId(branch.timeZoneId ?? "");
    setAddressLine1(branch.addressLine1 ?? "");
    setAddressLine2(branch.addressLine2 ?? "");
    setCity(branch.city ?? "");
    setRegion(branch.region ?? "");
    setPostalCode(branch.postalCode ?? "");
    setCountryCode(branch.countryCode ?? "PH");
    setLatitude(formatCoordinate(branch.latitude));
    setLongitude(formatCoordinate(branch.longitude));
    const policy = branch.deliveryPolicy;
    setMinimumOrder(String(policy?.minimumOrderAmount ?? 0));
    setBaseFee(String(policy?.baseDeliveryFee ?? 0));
    setIncludedKm(String(policy?.includedDistanceKm ?? 0));
    setAdditionalPerKm(String(policy?.additionalFeePerKm ?? 0));
    setMaximumKm(String(policy?.maximumDeliveryDistanceKm ?? 0));
    setFreeThreshold(
      policy?.freeDeliveryThreshold == null ? "" : String(policy.freeDeliveryThreshold),
    );
  }

  const mapLinks = useMemo(() => {
    const lat = Number(latitude);
    const lng = Number(longitude);
    return externalMapLinks(Number.isFinite(lat) ? lat : null, Number.isFinite(lng) ? lng : null);
  }, [latitude, longitude]);

  if (!canManage) {
    return (
      <div
        data-testid="branch-fulfillment-denied"
        className="branch-fulfillment-edit-page exits-page flex min-w-0 flex-col gap-3"
      >
        <PageHeader
          title={t("branches.editTitle")}
          description={t("branches.denied")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
      </div>
    );
  }

  if (!organizationId || detailQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (detailQuery.isError || !detailQuery.data?.branch) {
    return (
      <div
        data-testid="branch-fulfillment-not-found"
        className="branch-fulfillment-edit-page exits-page flex min-w-0 flex-col gap-3"
      >
        <PageHeader
          title={t("branches.editTitle")}
          description={t("branches.editLede")}
          backTo={pageBackNav.orgBranches.to}
          backLabel={t(pageBackNav.orgBranches.labelKey)}
          backTestId="page-header-back-org"
        />
        <ErrorState title={t("branches.notFound")} detail={t("branches.editLede")} />
      </div>
    );
  }

  if (!hydrated) {
    return <LoadingState label={t("loading.label")} />;
  }

  const branch = detailQuery.data.branch;
  const currentReadiness = readiness ?? detailQuery.data.readiness;
  const orderingLabel = orderingEnablementLabel(currentReadiness);
  const pickupLabel = pickupEnablementLabel(currentReadiness);
  const deliveryLabel = deliveryEnablementLabel(currentReadiness);
  const missingRequirements = currentReadiness.missingRequirements;
  const extraReasonCodes = filterRedundantReasonCodes(
    missingRequirements,
    currentReadiness.reasonCodes,
  );
  const setupComplete = missingRequirements.length === 0;

  async function saveAll() {
    if (!organizationId || busy) {
      return;
    }
    const coords = parseOptionalCoordinatePair(latitude, longitude);
    if (!coords.ok) {
      if (coords.error === "invalid_latitude") {
        setError(t("branches.invalidLatitude"));
      } else if (coords.error === "invalid_longitude") {
        setError(t("branches.invalidLongitude"));
      } else {
        setError(t("branches.coordsPairRequired"));
      }
      return;
    }
    if (!name.trim()) {
      setError(t("branches.nameRequired"));
      return;
    }

    setBusy(true);
    setError(null);
    setOkMessage(null);
    try {
      const updated = await updateOrganizationBranch(organizationId, branchId, {
        name: name.trim(),
        addressLine1: addressLine1.trim() || null,
        addressLine2: addressLine2.trim() || null,
        city: city.trim() || null,
        region: region.trim() || null,
        postalCode: postalCode.trim() || null,
        countryCode: countryCode.trim() || null,
        latitude: coords.latitude,
        longitude: coords.longitude,
        clearCoordinates: coords.clearCoordinates,
        contactPhone: contactPhone.trim() || null,
        timeZoneId: timeZoneId.trim() || null,
      });
      applyBranch(updated);

      const hoursResult = await upsertBranchOperatingHours(organizationId, branchId, {
        days: hoursToRequest(hours),
      });
      setReadiness(hoursResult);

      await upsertBranchDeliveryPolicy(organizationId, branchId, {
        minimumOrderAmount: Number(minimumOrder) || 0,
        baseDeliveryFee: Number(baseFee) || 0,
        includedDistanceKm: Number(includedKm) || 0,
        additionalFeePerKm: Number(additionalPerKm) || 0,
        maximumDeliveryDistanceKm: Number(maximumKm) || 0,
        freeDeliveryThreshold: freeThreshold.trim() ? Number(freeThreshold) : null,
      });

      const refreshed = await getBranchFulfillmentReadiness(organizationId, branchId);
      setReadiness(refreshed);
      await queryClient.invalidateQueries({
        queryKey: ["branch-fulfillment-list", organizationId],
      });
      setOkMessage(t("branches.saved"));
    } catch (err) {
      setError(
        err instanceof PlatformApiError
          ? (err.problem.detail ?? t("branches.saveFailed"))
          : t("branches.saveFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  async function toggleFulfillment(partial: {
    customerOrderingEnabled?: boolean;
    pickupEnabled?: boolean;
    deliveryEnabled?: boolean;
  }) {
    if (!organizationId || busy) {
      return;
    }
    setBusy(true);
    setError(null);
    setOkMessage(null);
    try {
      const next = await updateBranchFulfillmentSettings(organizationId, branchId, partial);
      setReadiness(next);
      setOkMessage(t("branches.saved"));
    } catch (err) {
      setError(
        err instanceof PlatformApiError
          ? (err.problem.detail ?? t("branches.fulfillmentFailed"))
          : t("branches.fulfillmentFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  async function pauseOrders(paused: boolean) {
    if (!organizationId || busy) {
      return;
    }
    setBusy(true);
    setError(null);
    setOkMessage(null);
    try {
      const next = await setBranchOnlineOrdersPaused(organizationId, branchId, {
        paused,
        reason: paused ? "TooBusy" : null,
      });
      setReadiness(next);
      setOkMessage(t("branches.saved"));
    } catch (err) {
      setError(
        err instanceof PlatformApiError
          ? (err.problem.detail ?? t("branches.fulfillmentFailed"))
          : t("branches.fulfillmentFailed"),
      );
    } finally {
      setBusy(false);
    }
  }

  async function captureGpsOnce() {
    if (gpsBusy) {
      return;
    }
    setGpsBusy(true);
    setError(null);
    const result = await requestGpsAssistOnce();
    setGpsBusy(false);
    if (!result.ok) {
      setError(t(`branches.gps.${result.error}` as MessageKey));
      return;
    }
    setLatitude(formatCoordinate(result.latitude));
    setLongitude(formatCoordinate(result.longitude));
  }

  function updateHour(dayOfWeek: string, patch: Partial<HoursDayDraft>) {
    setHours((prev) =>
      prev.map((day) => (day.dayOfWeek === dayOfWeek ? { ...day, ...patch } : day)),
    );
  }

  return (
    <div
      className="branch-fulfillment-edit-page exits-page flex min-w-0 flex-col gap-3 pb-4"
      data-testid="branch-fulfillment-edit"
    >
      <PageHeader
        title={branch.name}
        description={t("branches.editLede")}
        backTo={pageBackNav.orgBranches.to}
        backLabel={t(pageBackNav.orgBranches.labelKey)}
        backTestId="page-header-back-org"
      />

      {error ? (
        <div
          className="exits-alert exits-alert--error"
          role="alert"
          data-testid="branch-fulfillment-error"
        >
          <div className="flex gap-3">
            <CircleAlert className="mt-0.5 size-5 shrink-0" aria-hidden />
            <p className="m-0 text-[length:var(--exits-text-sm)]">{error}</p>
          </div>
        </div>
      ) : null}
      {okMessage ? (
        <div
          className="exits-alert exits-alert--success"
          role="status"
          data-testid="branch-fulfillment-ok"
        >
          <div className="flex gap-3">
            <CircleCheck className="mt-0.5 size-5 shrink-0" aria-hidden />
            <p className="m-0 text-[length:var(--exits-text-sm)]">{okMessage}</p>
          </div>
        </div>
      ) : null}

      <section
        className="catalog-form-section exits-animate-panel branch-readiness gap-3"
        data-testid="branch-readiness-panel"
      >
        <div className="branch-readiness__header">
          <h2 className="catalog-form-section__title">{t("branches.readinessTitle")}</h2>
          {currentReadiness.storeStatusMessage ? (
            <p className="branch-readiness__store-status m-0 text-[length:var(--exits-text-sm)] text-muted">
              {currentReadiness.storeStatusMessage}
            </p>
          ) : null}
        </div>

        <div className="branch-readiness__channels" role="list">
          {(
            [
              { kind: "ordering" as const, label: orderingLabel, testId: "ordering-status" },
              { kind: "pickup" as const, label: pickupLabel, testId: "pickup-status" },
              { kind: "delivery" as const, label: deliveryLabel, testId: "delivery-status" },
            ] as const
          ).map((channel) => (
            <div
              key={channel.kind}
              className="branch-readiness__channel"
              role="listitem"
              data-testid={channel.testId}
            >
              <span className="branch-readiness__channel-label">
                {t(enablementChannelKey(channel.kind))}
              </span>
              <StatusChip tone={enablementTone(channel.label)}>
                {t(enablementStatusWord(channel.label))}
              </StatusChip>
            </div>
          ))}
        </div>

        {!setupComplete ? (
          <div
            className="branch-readiness__checklist"
            data-testid="branch-missing-requirements"
          >
            <p className="branch-readiness__checklist-title m-0">
              {t("branches.setupGapsTitle")}
            </p>
            <p className="branch-readiness__checklist-lede m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("branches.setupGapsLede")}
            </p>
            <ul className="branch-readiness__items m-0 list-none p-0">
              {missingRequirements.map((code) => (
                <li key={code} className="branch-readiness__item">
                  <CircleAlert className="branch-readiness__item-icon" aria-hidden />
                  <span>{t(missingRequirementMessageKey(code))}</span>
                </li>
              ))}
            </ul>
          </div>
        ) : (
          <div
            className="branch-readiness__ready"
            data-testid="branch-missing-none"
            role="status"
          >
            <CircleCheck className="branch-readiness__ready-icon" aria-hidden />
            <p className="m-0 text-[length:var(--exits-text-sm)]">{t("branches.missingNone")}</p>
          </div>
        )}

        {extraReasonCodes.length > 0 ? (
          <div
            className="branch-readiness__checklist branch-readiness__checklist--secondary"
            data-testid="branch-reason-codes"
          >
            <p className="branch-readiness__checklist-title m-0">
              {t("branches.enablementGapsTitle")}
            </p>
            <ul className="branch-readiness__items m-0 list-none p-0">
              {extraReasonCodes.map((code) => (
                <li key={code} className="branch-readiness__item branch-readiness__item--muted">
                  <span className="branch-readiness__item-dot" aria-hidden />
                  <span>{t(reasonCodeMessageKey(code))}</span>
                </li>
              ))}
            </ul>
          </div>
        ) : null}
      </section>

      <section className="catalog-form-section exits-animate-panel gap-3">
        <h2 className="catalog-form-section__title">{t("branches.detailsTitle")}</h2>
        <div className="catalog-form-section__grid">
          <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.name")}
            <input
              className="catalog-form-select font-normal"
              value={name}
              onChange={(e) => setName(e.target.value)}
              data-testid="branch-name"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.contactPhone")}
            <input
              className="catalog-form-select font-normal"
              value={contactPhone}
              onChange={(e) => setContactPhone(e.target.value)}
              data-testid="branch-phone"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.timeZone")}
            <input
              className="catalog-form-select font-normal"
              value={timeZoneId}
              onChange={(e) => setTimeZoneId(e.target.value)}
              placeholder="Asia/Manila"
              data-testid="branch-timezone"
            />
          </label>
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel gap-3"
        data-testid="branch-address-section"
      >
        <h2 className="catalog-form-section__title">{t("branches.addressTitle")}</h2>
        <div className="catalog-form-section__grid">
          <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.addressLine1")}
            <input
              className="catalog-form-select font-normal"
              value={addressLine1}
              onChange={(e) => setAddressLine1(e.target.value)}
              data-testid="branch-address1"
            />
          </label>
          <label className="catalog-form-field--full flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.addressLine2")}
            <input
              className="catalog-form-select font-normal"
              value={addressLine2}
              onChange={(e) => setAddressLine2(e.target.value)}
              data-testid="branch-address2"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.city")}
            <input
              className="catalog-form-select font-normal"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              data-testid="branch-city"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.region")}
            <input
              className="catalog-form-select font-normal"
              value={region}
              onChange={(e) => setRegion(e.target.value)}
              data-testid="branch-region"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.postalCode")}
            <input
              className="catalog-form-select font-normal"
              value={postalCode}
              onChange={(e) => setPostalCode(e.target.value)}
              data-testid="branch-postal"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.countryCode")}
            <input
              className="catalog-form-select font-normal"
              value={countryCode}
              onChange={(e) => setCountryCode(e.target.value)}
              data-testid="branch-country"
            />
          </label>
        </div>
      </section>

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
              onChange={(e) => setLatitude(e.target.value)}
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
              onChange={(e) => setLongitude(e.target.value)}
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
            onClick={() => void captureGpsOnce()}
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
                <a
                  href={mapLinks.osm}
                  target="_blank"
                  rel="noreferrer"
                  data-testid="branch-maps-osm"
                >
                  {t("branches.openOsm")}
                </a>
              </Button>
            </>
          ) : null}
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel gap-3"
        data-testid="branch-hours-section"
      >
        <h2 className="catalog-form-section__title">{t("branches.hoursTitle")}</h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {hasConfiguredHours(hours)
            ? t("branches.hoursConfigured")
            : t("branches.hoursNotConfigured")}
        </p>
        <ul className="m-0 flex list-none flex-col gap-2 p-0">
          {ORDERED_WEEKDAYS.map((dayName) => {
            const day = hours.find((h) => h.dayOfWeek === dayName)!;
            return (
              <li key={dayName} className="branch-hours-day">
                <p className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-semibold">
                  {t(dayLabelKey(dayName))}
                </p>
                <div className="flex flex-wrap gap-3">
                  <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                    <input
                      type="radio"
                      name={`hours-mode-${dayName}`}
                      checked={!day.isClosed && !day.isOpen24Hours}
                      onChange={() =>
                        updateHour(dayName, { isClosed: false, isOpen24Hours: false })
                      }
                      data-testid={`hours-open-${dayName}`}
                    />
                    {t("branches.hoursOpen")}
                  </label>
                  <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                    <input
                      type="radio"
                      name={`hours-mode-${dayName}`}
                      checked={day.isOpen24Hours && !day.isClosed}
                      onChange={() => updateHour(dayName, { isClosed: false, isOpen24Hours: true })}
                      data-testid={`hours-24h-${dayName}`}
                    />
                    {t("branches.hours24")}
                  </label>
                  <label className="flex items-center gap-2 text-[length:var(--exits-text-sm)]">
                    <input
                      type="radio"
                      name={`hours-mode-${dayName}`}
                      checked={day.isClosed}
                      onChange={() => updateHour(dayName, { isClosed: true, isOpen24Hours: false })}
                      data-testid={`hours-closed-${dayName}`}
                    />
                    {t("branches.hoursClosed")}
                  </label>
                </div>
                {!day.isClosed && !day.isOpen24Hours ? (
                  <div className="mt-2 grid gap-2 sm:grid-cols-2">
                    <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
                      {t("branches.hoursStart")}
                      <input
                        type="time"
                        className="catalog-form-select font-normal"
                        value={day.openTime}
                        onChange={(e) => updateHour(dayName, { openTime: e.target.value })}
                        data-testid={`hours-start-${dayName}`}
                      />
                    </label>
                    <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
                      {t("branches.hoursEnd")}
                      <input
                        type="time"
                        className="catalog-form-select font-normal"
                        value={day.closeTime}
                        onChange={(e) => updateHour(dayName, { closeTime: e.target.value })}
                        data-testid={`hours-end-${dayName}`}
                      />
                    </label>
                  </div>
                ) : null}
              </li>
            );
          })}
        </ul>
      </section>

      <section
        className="catalog-form-section exits-animate-panel gap-3"
        data-testid="branch-fulfillment-toggles"
      >
        <h2 className="catalog-form-section__title">{t("branches.fulfillmentTitle")}</h2>
        <div className="flex flex-wrap gap-2">
          {currentReadiness.canUseCustomerOrdering &&
          !currentReadiness.customerOrderingEnabled &&
          currentReadiness.customerOrderingReady ? (
            <Button
              type="button"
              className="min-h-11"
              disabled={busy}
              onClick={() => void toggleFulfillment({ customerOrderingEnabled: true })}
              data-testid="enable-ordering"
            >
              {t("branches.enableOrdering")}
            </Button>
          ) : null}
          {currentReadiness.customerOrderingEnabled && !currentReadiness.onlineOrdersPaused ? (
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              disabled={busy}
              onClick={() => void pauseOrders(true)}
              data-testid="pause-ordering"
            >
              {t("branches.pauseOrders")}
            </Button>
          ) : null}
          {currentReadiness.onlineOrdersPaused ? (
            <Button
              type="button"
              className="min-h-11"
              disabled={busy}
              onClick={() => void pauseOrders(false)}
              data-testid="resume-ordering"
            >
              {t("branches.resumeOrders")}
            </Button>
          ) : null}
          {currentReadiness.canUseCustomerOrdering &&
          currentReadiness.customerOrderingEnabled &&
          currentReadiness.pickupReady &&
          !currentReadiness.pickupEnabled ? (
            <Button
              type="button"
              className="min-h-11"
              disabled={busy}
              onClick={() => void toggleFulfillment({ pickupEnabled: true })}
              data-testid="enable-pickup"
            >
              {t("branches.enablePickup")}
            </Button>
          ) : null}
          {currentReadiness.canUseDelivery &&
          currentReadiness.deliveryReady &&
          !currentReadiness.deliveryEnabled ? (
            <Button
              type="button"
              className="min-h-11"
              disabled={busy}
              onClick={() => void toggleFulfillment({ deliveryEnabled: true })}
              data-testid="enable-delivery"
            >
              {t("branches.enableDelivery")}
            </Button>
          ) : null}
          {currentReadiness.deliveryEnabled ? (
            <Button
              type="button"
              variant="outline"
              className="min-h-11"
              disabled={busy}
              onClick={() => void toggleFulfillment({ deliveryEnabled: false })}
              data-testid="disable-delivery"
            >
              {t("branches.disableDelivery")}
            </Button>
          ) : null}
        </div>
      </section>

      <section
        className="catalog-form-section exits-animate-panel gap-3"
        data-testid="branch-delivery-policy"
      >
        <h2 className="catalog-form-section__title">{t("branches.deliveryPolicyTitle")}</h2>
        <div className="catalog-form-section__grid">
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.minimumOrder")}
            <input
              type="number"
              step="any"
              className="catalog-form-select font-normal"
              value={minimumOrder}
              onChange={(e) => setMinimumOrder(e.target.value)}
              data-testid="policy-minimum"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.baseFee")}
            <input
              type="number"
              step="any"
              className="catalog-form-select font-normal"
              value={baseFee}
              onChange={(e) => setBaseFee(e.target.value)}
              data-testid="policy-base-fee"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.includedKm")}
            <input
              type="number"
              step="any"
              className="catalog-form-select font-normal"
              value={includedKm}
              onChange={(e) => setIncludedKm(e.target.value)}
              data-testid="policy-included-km"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.additionalPerKm")}
            <input
              type="number"
              step="any"
              className="catalog-form-select font-normal"
              value={additionalPerKm}
              onChange={(e) => setAdditionalPerKm(e.target.value)}
              data-testid="policy-additional-km"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.maximumKm")}
            <input
              type="number"
              step="any"
              className="catalog-form-select font-normal"
              value={maximumKm}
              onChange={(e) => setMaximumKm(e.target.value)}
              data-testid="policy-maximum-km"
            />
          </label>
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("branches.freeThreshold")}
            <input
              type="number"
              step="any"
              className="catalog-form-select font-normal"
              value={freeThreshold}
              onChange={(e) => setFreeThreshold(e.target.value)}
              data-testid="policy-free-threshold"
            />
          </label>
        </div>
      </section>

      <div
        className="catalog-form-actions branch-fulfillment-actions"
        role="region"
        aria-label={t("branches.saveActions")}
      >
        <div className="catalog-form-actions__primary">
          <Button
            type="button"
            className="catalog-form-actions__save"
            disabled={busy}
            onClick={() => void saveAll()}
            data-testid="branch-save"
          >
            {busy ? (
              <>
                <Loader2 className="size-4 shrink-0 animate-spin" aria-hidden />
                {t("branches.saving")}
              </>
            ) : (
              <>
                <Save className="size-4 shrink-0" aria-hidden />
                {t("branches.save")}
              </>
            )}
          </Button>
        </div>
        <div className="catalog-form-actions__secondary">
          <Button asChild variant="outline" className="min-h-11 w-full sm:w-auto">
            <Link to="/org/branches">{t("branches.cancel")}</Link>
          </Button>
        </div>
      </div>
    </div>
  );
}
