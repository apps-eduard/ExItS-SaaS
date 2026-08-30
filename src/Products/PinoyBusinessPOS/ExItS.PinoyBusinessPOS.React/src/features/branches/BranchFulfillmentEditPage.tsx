import { useEffect, useMemo, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { Check, CircleAlert, CircleCheck, Loader2, Save } from "lucide-react";
import { canManageBranchFulfillment } from "@/access/pos-capabilities";
import {
  addBranchDeliveryServiceArea,
  deleteBranchDeliveryServiceArea,
  getBranchFulfillmentReadiness,
  getBranchOperatingHours,
  listBranchDeliveryServiceAreas,
  listOrganizationBranchesForFulfillment,
  setBranchOnlineOrdersPaused,
  updateBranchFulfillmentSettings,
  updateOrganizationBranch,
  upsertBranchDeliveryPolicy,
  upsertBranchOperatingHours,
  type BranchDeliveryServiceAreaDto,
  type BranchFulfillmentReadinessDto,
  type OrganizationBranchDto,
} from "@/api/platform/branch-fulfillment-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { pageBackNav } from "@/navigation/page-back-nav";
import {
  formatCoordinate,
  isMapProviderConfigured,
  parseOptionalCoordinatePair,
} from "@/features/branches/branch-coordinates";
import {
  defaultHoursSchedule,
  hoursFromDto,
  hoursToRequest,
  type HoursDayDraft,
} from "@/features/branches/branch-hours";
import { externalMapLinks, requestGpsAssistOnce } from "@/features/branches/branch-map-links";
import { BranchDeliveryAreasPanel } from "@/features/branches/BranchDeliveryAreasPanel";
import { BranchDeliveryLocationForm } from "@/features/branches/BranchDeliveryLocationForm";
import { BranchDeliveryPolicyForm } from "@/features/branches/BranchDeliveryPolicyForm";
import { BranchDetailsForm } from "@/features/branches/BranchDetailsForm";
import { BranchHoursForm } from "@/features/branches/BranchHoursForm";
import { BranchOverviewPanel } from "@/features/branches/BranchOverviewPanel";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export type BranchSetupTab =
  | "overview"
  | "details"
  | "hours"
  | "location"
  | "policy"
  | "areas";

function TabCompleteIcon({ complete }: { complete: boolean }) {
  if (!complete) {
    return null;
  }
  return <Check className="size-3.5 shrink-0 text-[color:var(--exits-success)]" aria-hidden />;
}

export function BranchFulfillmentEditPage() {
  const { t } = useI18n();
  const { branchId = "" } = useParams();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManageBranchFulfillment(sessionGrant);
  const organizationId = boundWorkspace?.organizationId;
  const mapProviderReady = isMapProviderConfigured();

  const [activeTab, setActiveTab] = useState<BranchSetupTab>("overview");

  const detailQuery = useQuery({
    queryKey: ["branch-fulfillment-detail", organizationId, branchId],
    enabled: Boolean(organizationId && branchId && canManage),
    queryFn: async ({ signal }) => {
      const [branches, readiness, hours, areas] = await Promise.all([
        listOrganizationBranchesForFulfillment(organizationId!, signal),
        getBranchFulfillmentReadiness(organizationId!, branchId, signal),
        getBranchOperatingHours(organizationId!, branchId, signal),
        listBranchDeliveryServiceAreas(organizationId!, branchId, signal),
      ]);
      const branch = branches.find((b) => b.id === branchId) ?? null;
      return { branch, readiness, hours, areas };
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
  const [areas, setAreas] = useState<BranchDeliveryServiceAreaDto[]>([]);
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
    setAreas(data.areas);
    setHydrated(true);
  }, [detailQuery.data, hydrated]);

  useEffect(() => {
    setHydrated(false);
    setActiveTab("overview");
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

  async function refreshAreasAndReadiness() {
    if (!organizationId) return;
    const [nextAreas, nextReadiness] = await Promise.all([
      listBranchDeliveryServiceAreas(organizationId, branchId),
      getBranchFulfillmentReadiness(organizationId, branchId),
    ]);
    setAreas(nextAreas);
    setReadiness(nextReadiness);
  }

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
      await queryClient.invalidateQueries({
        queryKey: ["branch-fulfillment-detail", organizationId, branchId],
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
      await queryClient.invalidateQueries({
        queryKey: ["branch-fulfillment-list", organizationId],
      });
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

  const showSave =
    activeTab === "details" ||
    activeTab === "hours" ||
    activeTab === "location" ||
    activeTab === "policy";

  const tabItems = [
    {
      key: "overview",
      label: (
        <span className="branch-setup-tab-label">
          {t("branches.tab.overview")}
        </span>
      ),
      testId: "branch-tab-overview",
    },
    {
      key: "details",
      label: (
        <span className="branch-setup-tab-label">
          <TabCompleteIcon complete={currentReadiness.branchDetailsComplete} />
          {t("branches.tab.details")}
        </span>
      ),
      testId: "branch-tab-details",
    },
    {
      key: "hours",
      label: (
        <span className="branch-setup-tab-label">
          <TabCompleteIcon complete={currentReadiness.operatingHoursComplete} />
          {t("branches.tab.hours")}
        </span>
      ),
      testId: "branch-tab-hours",
    },
    {
      key: "location",
      label: (
        <span className="branch-setup-tab-label">
          <TabCompleteIcon complete={currentReadiness.deliveryLocationComplete} />
          {t("branches.tab.location")}
        </span>
      ),
      testId: "branch-tab-location",
    },
    {
      key: "policy",
      label: (
        <span className="branch-setup-tab-label">
          <TabCompleteIcon complete={currentReadiness.deliveryPolicyComplete} />
          {t("branches.tab.policy")}
        </span>
      ),
      testId: "branch-tab-policy",
    },
    {
      key: "areas",
      label: (
        <span className="branch-setup-tab-label">
          <TabCompleteIcon complete={currentReadiness.deliveryAreasComplete} />
          {t("branches.tab.areas")}
        </span>
      ),
      testId: "branch-tab-areas",
    },
  ];

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

      <div className="branch-setup-tabs-scroll">
        <UnderlineTabBar
          items={tabItems}
          activeKey={activeTab}
          onChange={(key) => setActiveTab(key as BranchSetupTab)}
          ariaLabel={t("branches.setupTabsLabel")}
          testId="branch-setup-tabs"
          className="branch-setup-tabs"
        />
      </div>

      {activeTab === "overview" ? (
        <BranchOverviewPanel
          readiness={currentReadiness}
          busy={busy}
          t={t}
          onTogglePickup={(enabled) => void toggleFulfillment({ pickupEnabled: enabled })}
          onToggleDelivery={(enabled) => void toggleFulfillment({ deliveryEnabled: enabled })}
          onEnableOrdering={() => void toggleFulfillment({ customerOrderingEnabled: true })}
          onPauseOrders={() => void pauseOrders(true)}
          onResumeOrders={() => void pauseOrders(false)}
        />
      ) : null}

      {activeTab === "details" ? (
        <BranchDetailsForm
          name={name}
          contactPhone={contactPhone}
          timeZoneId={timeZoneId}
          addressLine1={addressLine1}
          addressLine2={addressLine2}
          city={city}
          region={region}
          postalCode={postalCode}
          countryCode={countryCode}
          t={t}
          onChange={(field, value) => {
            if (field === "name") setName(value);
            else if (field === "contactPhone") setContactPhone(value);
            else if (field === "timeZoneId") setTimeZoneId(value);
            else if (field === "addressLine1") setAddressLine1(value);
            else if (field === "addressLine2") setAddressLine2(value);
            else if (field === "city") setCity(value);
            else if (field === "region") setRegion(value);
            else if (field === "postalCode") setPostalCode(value);
            else if (field === "countryCode") setCountryCode(value);
          }}
        />
      ) : null}

      {activeTab === "hours" ? (
        <BranchHoursForm hours={hours} t={t} onUpdateHour={updateHour} />
      ) : null}

      {activeTab === "location" ? (
        <BranchDeliveryLocationForm
          latitude={latitude}
          longitude={longitude}
          mapProviderReady={mapProviderReady}
          mapLinks={mapLinks}
          gpsBusy={gpsBusy}
          busy={busy}
          t={t}
          onLatitudeChange={setLatitude}
          onLongitudeChange={setLongitude}
          onCaptureGps={() => void captureGpsOnce()}
        />
      ) : null}

      {activeTab === "policy" ? (
        <BranchDeliveryPolicyForm
          minimumOrder={minimumOrder}
          baseFee={baseFee}
          includedKm={includedKm}
          additionalPerKm={additionalPerKm}
          maximumKm={maximumKm}
          freeThreshold={freeThreshold}
          t={t}
          onChange={(field, value) => {
            if (field === "minimumOrder") setMinimumOrder(value);
            else if (field === "baseFee") setBaseFee(value);
            else if (field === "includedKm") setIncludedKm(value);
            else if (field === "additionalPerKm") setAdditionalPerKm(value);
            else if (field === "maximumKm") setMaximumKm(value);
            else if (field === "freeThreshold") setFreeThreshold(value);
          }}
        />
      ) : null}

      {activeTab === "areas" ? (
        <BranchDeliveryAreasPanel
          areas={areas}
          busy={busy}
          t={t}
          onAdd={async (input) => {
            if (!organizationId || busy) return;
            setBusy(true);
            setError(null);
            setOkMessage(null);
            try {
              const next = await addBranchDeliveryServiceArea(organizationId, branchId, input);
              setReadiness(next);
              await refreshAreasAndReadiness();
              await queryClient.invalidateQueries({
                queryKey: ["branch-fulfillment-list", organizationId],
              });
              setOkMessage(t("branches.deliveryAreas.added"));
            } catch (err) {
              setError(
                err instanceof PlatformApiError
                  ? (err.problem.detail ?? t("branches.deliveryAreas.addFailed"))
                  : t("branches.deliveryAreas.addFailed"),
              );
            } finally {
              setBusy(false);
            }
          }}
          onRemove={async (areaId) => {
            if (!organizationId || busy) return;
            setBusy(true);
            setError(null);
            setOkMessage(null);
            try {
              const next = await deleteBranchDeliveryServiceArea(
                organizationId,
                branchId,
                areaId,
              );
              setReadiness(next);
              await refreshAreasAndReadiness();
              await queryClient.invalidateQueries({
                queryKey: ["branch-fulfillment-list", organizationId],
              });
              setOkMessage(t("branches.deliveryAreas.removed"));
            } catch (err) {
              setError(
                err instanceof PlatformApiError
                  ? (err.problem.detail ?? t("branches.deliveryAreas.removeFailed"))
                  : t("branches.deliveryAreas.removeFailed"),
              );
            } finally {
              setBusy(false);
            }
          }}
        />
      ) : null}

      {showSave ? (
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
      ) : null}
    </div>
  );
}
