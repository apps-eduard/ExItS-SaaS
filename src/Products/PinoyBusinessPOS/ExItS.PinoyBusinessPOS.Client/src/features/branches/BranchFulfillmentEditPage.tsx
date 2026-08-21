import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
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
import { Card } from "@/components/ui/card";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
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
  if (label === "paused") return "warning";
  if (label === "notReady") return "danger";
  return "info";
}

function enablementKey(
  kind: "ordering" | "pickup" | "delivery",
  label: EnablementLabel,
): MessageKey {
  if (kind === "ordering") {
    if (label === "enabled") return "branches.orderingEnabled";
    if (label === "paused") return "branches.orderingPaused";
    if (label === "notReady") return "branches.orderingNotReady";
    return "branches.orderingDisabled";
  }
  if (kind === "pickup") {
    if (label === "enabled") return "branches.pickupEnabled";
    if (label === "notReady") return "branches.pickupNotReady";
    return "branches.pickupDisabled";
  }
  if (label === "enabled") return "branches.deliveryEnabled";
  if (label === "notReady") return "branches.deliveryNotReady";
  return "branches.deliveryDisabled";
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
      <div data-testid="branch-fulfillment-denied" className="flex flex-col gap-3">
        <PageHeader title={t("branches.editTitle")} description={t("branches.denied")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/org">{t("branches.backOrg")}</Link>
        </Button>
      </div>
    );
  }

  if (!organizationId || detailQuery.isLoading) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  if (detailQuery.isError || !detailQuery.data?.branch) {
    return (
      <div data-testid="branch-fulfillment-not-found" className="flex flex-col gap-3">
        <PageHeader title={t("branches.editTitle")} description={t("branches.notFound")} />
        <Button asChild variant="ghost" className="min-h-11 w-fit">
          <Link to="/org/branches">{t("branches.backList")}</Link>
        </Button>
      </div>
    );
  }

  if (!hydrated) {
    return <LoadingSkeleton label={t("loading.label")} />;
  }

  const branch = detailQuery.data.branch;
  const currentReadiness = readiness ?? detailQuery.data.readiness;
  const orderingLabel = orderingEnablementLabel(currentReadiness);
  const pickupLabel = pickupEnablementLabel(currentReadiness);
  const deliveryLabel = deliveryEnablementLabel(currentReadiness);

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

  async function useGpsOnce() {
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
    <div className="flex min-w-0 flex-col gap-4 pb-24" data-testid="branch-fulfillment-edit">
      <PageHeader title={branch.name} description={t("branches.editLede")} />
      <Button asChild variant="ghost" className="min-h-11 w-fit">
        <Link to="/org/branches">{t("branches.backList")}</Link>
      </Button>

      {error ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-danger"
          role="alert"
          data-testid="branch-fulfillment-error"
        >
          {error}
        </p>
      ) : null}
      {okMessage ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-success"
          data-testid="branch-fulfillment-ok"
        >
          {okMessage}
        </p>
      ) : null}

      <Card className="flex flex-col gap-3" data-testid="branch-readiness-panel">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("branches.readinessTitle")}
        </h2>
        {currentReadiness.storeStatusMessage ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {currentReadiness.storeStatusMessage}
          </p>
        ) : null}
        <div className="flex flex-wrap gap-2">
          <span data-testid="ordering-status">
            <StatusChip tone={enablementTone(orderingLabel)}>
              {t(enablementKey("ordering", orderingLabel))}
            </StatusChip>
          </span>
          <span data-testid="pickup-status">
            <StatusChip tone={enablementTone(pickupLabel)}>
              {t(enablementKey("pickup", pickupLabel))}
            </StatusChip>
          </span>
          <span data-testid="delivery-status">
            <StatusChip tone={enablementTone(deliveryLabel)}>
              {t(enablementKey("delivery", deliveryLabel))}
            </StatusChip>
          </span>
        </div>
        {currentReadiness.missingRequirements.length > 0 ? (
          <div data-testid="branch-missing-requirements">
            <p className="m-0 mb-2 text-[length:var(--exits-text-sm)] font-medium">
              {t("branches.missingTitle")}
            </p>
            <ul className="m-0 list-disc ps-5 text-[length:var(--exits-text-sm)]">
              {currentReadiness.missingRequirements.map((code) => (
                <li key={code}>{t(missingRequirementMessageKey(code))}</li>
              ))}
            </ul>
          </div>
        ) : (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="branch-missing-none"
          >
            {t("branches.missingNone")}
          </p>
        )}
        {currentReadiness.reasonCodes.length > 0 ? (
          <ul
            className="m-0 list-disc ps-5 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="branch-reason-codes"
          >
            {currentReadiness.reasonCodes.map((code) => (
              <li key={code}>{t(reasonCodeMessageKey(code))}</li>
            ))}
          </ul>
        ) : null}
      </Card>

      <Card className="flex flex-col gap-3">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("branches.detailsTitle")}
        </h2>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("branches.name")}
          <input
            className="min-h-11 rounded-md border border-border bg-surface px-3"
            value={name}
            onChange={(e) => setName(e.target.value)}
            data-testid="branch-name"
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("branches.contactPhone")}
          <input
            className="min-h-11 rounded-md border border-border bg-surface px-3"
            value={contactPhone}
            onChange={(e) => setContactPhone(e.target.value)}
            data-testid="branch-phone"
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("branches.timeZone")}
          <input
            className="min-h-11 rounded-md border border-border bg-surface px-3"
            value={timeZoneId}
            onChange={(e) => setTimeZoneId(e.target.value)}
            placeholder="Asia/Manila"
            data-testid="branch-timezone"
          />
        </label>
      </Card>

      <Card className="flex flex-col gap-3" data-testid="branch-address-section">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("branches.addressTitle")}
        </h2>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("branches.addressLine1")}
          <input
            className="min-h-11 rounded-md border border-border bg-surface px-3"
            value={addressLine1}
            onChange={(e) => setAddressLine1(e.target.value)}
            data-testid="branch-address1"
          />
        </label>
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("branches.addressLine2")}
          <input
            className="min-h-11 rounded-md border border-border bg-surface px-3"
            value={addressLine2}
            onChange={(e) => setAddressLine2(e.target.value)}
            data-testid="branch-address2"
          />
        </label>
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.city")}
            <input
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={city}
              onChange={(e) => setCity(e.target.value)}
              data-testid="branch-city"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.region")}
            <input
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={region}
              onChange={(e) => setRegion(e.target.value)}
              data-testid="branch-region"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.postalCode")}
            <input
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={postalCode}
              onChange={(e) => setPostalCode(e.target.value)}
              data-testid="branch-postal"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.countryCode")}
            <input
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={countryCode}
              onChange={(e) => setCountryCode(e.target.value)}
              data-testid="branch-country"
            />
          </label>
        </div>
      </Card>

      <Card className="flex flex-col gap-3" data-testid="branch-map-section">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("branches.mapTitle")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">{t("branches.mapHint")}</p>
        {!mapProviderReady ? (
          <p
            className="m-0 rounded-md border border-border bg-muted/30 p-3 text-[length:var(--exits-text-sm)]"
            data-testid="branch-map-fallback"
          >
            {t("branches.mapFallback")}
          </p>
        ) : (
          <p
            className="m-0 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="branch-map-provider-ready"
          >
            {t("branches.mapProviderReady")}
          </p>
        )}
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.latitude")}
            <input
              type="number"
              step="any"
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={latitude}
              onChange={(e) => setLatitude(e.target.value)}
              data-testid="branch-latitude"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.longitude")}
            <input
              type="number"
              step="any"
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={longitude}
              onChange={(e) => setLongitude(e.target.value)}
              data-testid="branch-longitude"
            />
          </label>
        </div>
        <div className="flex flex-wrap gap-2">
          <Button
            type="button"
            variant="ghost"
            className="min-h-11"
            disabled={gpsBusy || busy}
            onClick={() => void useGpsOnce()}
            data-testid="branch-gps-assist"
          >
            {gpsBusy ? t("branches.gpsWorking") : t("branches.gpsAssist")}
          </Button>
          {mapLinks ? (
            <>
              <Button asChild variant="ghost" className="min-h-11">
                <a
                  href={mapLinks.google}
                  target="_blank"
                  rel="noreferrer"
                  data-testid="branch-maps-google"
                >
                  {t("branches.openGoogleMaps")}
                </a>
              </Button>
              <Button asChild variant="ghost" className="min-h-11">
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
      </Card>

      <Card className="flex flex-col gap-3" data-testid="branch-hours-section">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("branches.hoursTitle")}
        </h2>
        <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
          {hasConfiguredHours(hours)
            ? t("branches.hoursConfigured")
            : t("branches.hoursNotConfigured")}
        </p>
        <ul className="m-0 flex list-none flex-col gap-3 p-0">
          {ORDERED_WEEKDAYS.map((dayName) => {
            const day = hours.find((h) => h.dayOfWeek === dayName)!;
            return (
              <li key={dayName} className="rounded-md border border-border p-3">
                <p className="m-0 mb-2 font-medium">{t(dayLabelKey(dayName))}</p>
                <div className="flex flex-wrap gap-2">
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
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      {t("branches.hoursStart")}
                      <input
                        type="time"
                        className="min-h-11 rounded-md border border-border bg-surface px-3"
                        value={day.openTime}
                        onChange={(e) => updateHour(dayName, { openTime: e.target.value })}
                        data-testid={`hours-start-${dayName}`}
                      />
                    </label>
                    <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                      {t("branches.hoursEnd")}
                      <input
                        type="time"
                        className="min-h-11 rounded-md border border-border bg-surface px-3"
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
      </Card>

      <Card className="flex flex-col gap-3" data-testid="branch-fulfillment-toggles">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("branches.fulfillmentTitle")}
        </h2>
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
              variant="ghost"
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
              variant="ghost"
              className="min-h-11"
              disabled={busy}
              onClick={() => void toggleFulfillment({ deliveryEnabled: false })}
              data-testid="disable-delivery"
            >
              {t("branches.disableDelivery")}
            </Button>
          ) : null}
        </div>
      </Card>

      <Card className="flex flex-col gap-3" data-testid="branch-delivery-policy">
        <h2 className="m-0 text-[length:var(--exits-text-md)] font-medium">
          {t("branches.deliveryPolicyTitle")}
        </h2>
        <div className="grid gap-3 sm:grid-cols-2">
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.minimumOrder")}
            <input
              type="number"
              step="any"
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={minimumOrder}
              onChange={(e) => setMinimumOrder(e.target.value)}
              data-testid="policy-minimum"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.baseFee")}
            <input
              type="number"
              step="any"
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={baseFee}
              onChange={(e) => setBaseFee(e.target.value)}
              data-testid="policy-base-fee"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.includedKm")}
            <input
              type="number"
              step="any"
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={includedKm}
              onChange={(e) => setIncludedKm(e.target.value)}
              data-testid="policy-included-km"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.additionalPerKm")}
            <input
              type="number"
              step="any"
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={additionalPerKm}
              onChange={(e) => setAdditionalPerKm(e.target.value)}
              data-testid="policy-additional-km"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.maximumKm")}
            <input
              type="number"
              step="any"
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={maximumKm}
              onChange={(e) => setMaximumKm(e.target.value)}
              data-testid="policy-maximum-km"
            />
          </label>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("branches.freeThreshold")}
            <input
              type="number"
              step="any"
              className="min-h-11 rounded-md border border-border bg-surface px-3"
              value={freeThreshold}
              onChange={(e) => setFreeThreshold(e.target.value)}
              data-testid="policy-free-threshold"
            />
          </label>
        </div>
      </Card>

      <div
        className="sticky bottom-0 z-10 flex flex-wrap gap-2 border-t border-border bg-surface/95 p-3 backdrop-blur"
        role="region"
        aria-label={t("branches.saveActions")}
      >
        <Button asChild variant="ghost" className="min-h-11">
          <Link to="/org/branches">{t("branches.cancel")}</Link>
        </Button>
        <Button
          type="button"
          className="min-h-11"
          disabled={busy}
          onClick={() => void saveAll()}
          data-testid="branch-save"
        >
          {busy ? t("branches.saving") : t("branches.save")}
        </Button>
      </div>
    </div>
  );
}
