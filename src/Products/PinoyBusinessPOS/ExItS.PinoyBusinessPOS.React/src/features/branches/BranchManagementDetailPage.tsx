import { useEffect, useMemo, useState } from "react";
import { Link, useParams, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CircleAlert, Eye, EyeOff } from "lucide-react";
import {
  canInviteOrganizationStaff,
  canManageBranchFulfillment,
  canManageInventory,
  canUseWarehouseBranches,
  canViewInventory,
  canViewPurchasing,
} from "@/access/pos-capabilities";
import {
  issueBranchArchiveStepUp,
  issueBranchReactivateStepUp,
  issueBranchSetPrimaryStepUp,
  issueBranchSuspendStepUp,
  type GovernanceStepUpFailureReason,
} from "@/api/platform/governance-step-up-client";
import {
  archiveOrganizationBranch,
  getOrganizationBranch,
  listBranchManagementSummaries,
  reactivateOrganizationBranch,
  setPrimaryOrganizationBranch,
  suspendOrganizationBranch,
  updateOrganizationBranchDetails,
} from "@/api/platform/organization-branches-client";
import { listPosDevices } from "@/api/platform/pos-devices-client";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { BottomSheet, ConfirmationDialog } from "@/components/exits/SheetDialog";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { BranchDetailsForm } from "@/features/branches/BranchDetailsForm";
import { BranchStaffAccessPanel } from "@/features/branches/BranchStaffAccessPanel";
import { BranchStorefrontQrPanel } from "@/features/branches/BranchStorefrontQrPanel";
import { branchAdminCopy } from "@/features/branches/branch-admin-copy";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";
import {
  BRANCH_DEFAULT_COUNTRY_CODE,
  BRANCH_DEFAULT_TIME_ZONE,
} from "@/features/branches/branch-defaults";
import { branchFulfillmentEditPath } from "@/features/branches/branch-setup-tabs";
import { isWarehouseBranch } from "@/features/branches/branch-type";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const RETAIL_DETAIL_TABS = ["overview", "details", "staff", "devices", "fulfillment"] as const;
const WAREHOUSE_DETAIL_TABS = ["overview", "details", "staff", "devices"] as const;
type DetailTab = (typeof RETAIL_DETAIL_TABS)[number];

function parseDetailTab(
  value: string | null | undefined,
  warehouse: boolean,
): DetailTab {
  if (warehouse) {
    if (value === "fulfillment") {
      return "overview";
    }
    if (value && (WAREHOUSE_DETAIL_TABS as readonly string[]).includes(value)) {
      return value as DetailTab;
    }
    return "overview";
  }
  if (value && (RETAIL_DETAIL_TABS as readonly string[]).includes(value)) {
    return value as DetailTab;
  }
  return "overview";
}

type LifecycleAction = "suspend" | "reactivate" | "archive" | "set-primary";

const MIN_REASON_LENGTH = 8;

function statusLabel(status: string, t: (key: MessageKey) => string): string {
  switch (normalizeBranchStatusFilter(status)) {
    case "Active":
      return t("branches.mgmt.status.active");
    case "Suspended":
      return t("branches.mgmt.status.suspended");
    case "Archived":
      return t("branches.mgmt.status.archived");
    default:
      return status;
  }
}

function stepUpMessage(reason: GovernanceStepUpFailureReason, t: (key: MessageKey) => string): string {
  switch (reason) {
    case "password_required":
      return t("devices.revoke.passwordRequired");
    case "wrong_password":
      return t("devices.revoke.wrongPassword");
    case "expired":
      return t("devices.revoke.expired");
    case "consumed":
      return t("devices.revoke.consumed");
    case "invalid_scope":
      return t("devices.revoke.invalidScope");
    case "not_allowed":
      return t("devices.revoke.notAllowed");
    default:
      return t("devices.revoke.unavailable");
  }
}

export function BranchManagementDetailPage() {
  const { t } = useI18n();
  const { branchId = "" } = useParams();
  const [searchParams, setSearchParams] = useSearchParams();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManageBranchFulfillment(sessionGrant);
  const canGovern = canInviteOrganizationStaff(sessionGrant);
  const warehouseAllowed = canUseWarehouseBranches(sessionGrant);
  const canInventory = canViewInventory(sessionGrant);
  const canReceive = canManageInventory(sessionGrant);
  const canPurchasing = canViewPurchasing(sessionGrant);
  const organizationId = boundWorkspace?.organizationId ?? null;

  const [activeTab, setActiveTab] = useState<DetailTab>("overview");
  const [detailsDraft, setDetailsDraft] = useState<{
    name: string;
    contactPhone: string;
    addressLine1: string;
    addressLine2: string;
    city: string;
    region: string;
    postalCode: string;
    branchType: "Retail" | "Warehouse";
  }>({
    name: "",
    contactPhone: "",
    addressLine1: "",
    addressLine2: "",
    city: "",
    region: "",
    postalCode: "",
    branchType: "Retail",
  });
  const [detailsMessage, setDetailsMessage] = useState<string | null>(null);
  const [detailsError, setDetailsError] = useState<string | null>(null);

  const [lifecycleAction, setLifecycleAction] = useState<LifecycleAction | null>(null);
  const [lifecycleReason, setLifecycleReason] = useState("");
  const [lifecyclePassword, setLifecyclePassword] = useState("");
  const [lifecyclePasswordVisible, setLifecyclePasswordVisible] = useState(false);
  const [lifecycleError, setLifecycleError] = useState<string | null>(null);
  const [confirmPrimary, setConfirmPrimary] = useState(false);

  const branchQuery = useQuery({
    queryKey: ["branch-management-detail", organizationId, branchId],
    enabled: Boolean(organizationId && branchId && canManage),
    queryFn: async ({ signal }) => {
      const result = await getOrganizationBranch(organizationId!, branchId, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.mgmt.loadError"));
      }
      return result.value;
    },
  });

  useEffect(() => {
    const warehouse = isWarehouseBranch(branchQuery.data?.branchType);
    const nextTab = parseDetailTab(searchParams.get("tab"), warehouse);
    setActiveTab(nextTab);
    if (warehouse && searchParams.get("tab") === "fulfillment") {
      const next = new URLSearchParams(searchParams);
      next.delete("tab");
      setSearchParams(next, { replace: true });
    }
  }, [searchParams, branchQuery.data?.branchType, setSearchParams]);

  useEffect(() => {
    if (isWarehouseBranch(branchQuery.data?.branchType)) {
      return;
    }
    if (searchParams.get("focus") !== "qr" && window.location.hash !== "#branch-storefront-qr") {
      return;
    }
    if (activeTab !== "overview") {
      setActiveTab("overview");
      const next = new URLSearchParams(searchParams);
      next.delete("tab");
      setSearchParams(next, { replace: true });
      return;
    }
    // Wait until branch detail (and thus the QR panel) is mounted.
    if (!branchQuery.data) {
      return;
    }
    const handle = window.requestAnimationFrame(() => {
      document.getElementById("branch-storefront-qr")?.scrollIntoView({
        behavior: "smooth",
        block: "start",
      });
    });
    return () => window.cancelAnimationFrame(handle);
  }, [searchParams, branchId, activeTab, setSearchParams, branchQuery.data]);

  const summaryQuery = useQuery({
    queryKey: ["branch-management-summary", organizationId],
    enabled: Boolean(organizationId && canManage),
    queryFn: async ({ signal }) => {
      const result = await listBranchManagementSummaries(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.mgmt.loadError"));
      }
      return result.value;
    },
  });

  const devicesQuery = useQuery({
    queryKey: ["platform-pos-devices", organizationId],
    enabled: Boolean(organizationId && canManage && activeTab === "devices"),
    queryFn: async ({ signal }) => {
      const result = await listPosDevices(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("devices.loadError"));
      }
      return result.value;
    },
  });

  const branch = branchQuery.data;
  const summary = useMemo(
    () => (summaryQuery.data ?? []).find((item) => item.id === branchId) ?? null,
    [summaryQuery.data, branchId],
  );

  useEffect(() => {
    if (!branch) {
      return;
    }
    setDetailsDraft({
      name: branch.name,
      contactPhone: branch.contactPhone ?? "",
      addressLine1: branch.addressLine1 ?? "",
      addressLine2: branch.addressLine2 ?? "",
      city: branch.city ?? "",
      region: branch.region ?? "",
      postalCode: branch.postalCode ?? "",
      branchType: branch.branchType === "Warehouse" ? "Warehouse" : "Retail",
    });
  }, [branch]);

  const branchDevices = useMemo(
    () => (devicesQuery.data ?? []).filter((device) => device.branchId === branchId),
    [devicesQuery.data, branchId],
  );

  const statusKind = branch ? normalizeBranchStatusFilter(branch.status) : "Other";
  const reasonRequired =
    lifecycleAction === "suspend" ||
    lifecycleAction === "archive" ||
    lifecycleAction === "set-primary";

  function selectTab(tab: DetailTab) {
    const warehouse = isWarehouseBranch(branch?.branchType);
    const resolved = parseDetailTab(tab, warehouse);
    setActiveTab(resolved);
    const next = new URLSearchParams(searchParams);
    if (resolved === "overview") {
      next.delete("tab");
    } else {
      next.set("tab", resolved);
    }
    setSearchParams(next, { replace: true });
  }

  function closeLifecycle() {
    setLifecycleAction(null);
    setLifecycleReason("");
    setLifecyclePassword("");
    setLifecyclePasswordVisible(false);
    setLifecycleError(null);
  }

  const saveDetailsMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId || !branchId) {
        throw new Error(t("branches.saveFailed"));
      }
      if (!detailsDraft.name.trim()) {
        throw new Error(t("branches.nameRequired"));
      }
      const result = await updateOrganizationBranchDetails(organizationId, branchId, {
        name: detailsDraft.name.trim(),
        contactPhone: detailsDraft.contactPhone.trim() || null,
        addressLine1: detailsDraft.addressLine1.trim() || null,
        addressLine2: detailsDraft.addressLine2.trim() || null,
        city: detailsDraft.city.trim() || null,
        region: detailsDraft.region.trim() || null,
        postalCode: detailsDraft.postalCode.trim() || null,
        countryCode: BRANCH_DEFAULT_COUNTRY_CODE,
        timeZoneId: BRANCH_DEFAULT_TIME_ZONE,
        branchType: detailsDraft.branchType,
      });
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.saveFailed"));
      }
      return result.value;
    },
    onSuccess: async (updated) => {
      setDetailsError(null);
      setDetailsMessage(t(branchAdminCopy(updated.branchType).updatedMessage));
      await queryClient.invalidateQueries({
        queryKey: ["branch-management-detail", organizationId, branchId],
      });
      await queryClient.invalidateQueries({
        queryKey: ["branch-management-summary", organizationId],
      });
    },
    onError: (error) => {
      setDetailsMessage(null);
      setDetailsError(error instanceof Error ? error.message : t("branches.saveFailed"));
    },
  });

  const lifecycleMutation = useMutation({
    mutationFn: async () => {
      if (!organizationId || !branchId || !lifecycleAction) {
        throw new Error(t("branches.saveFailed"));
      }
      if (reasonRequired && lifecycleReason.trim().length < MIN_REASON_LENGTH) {
        throw new Error(t("devices.revoke.reasonTooShort"));
      }
      if (!lifecyclePassword.trim()) {
        throw new Error(t("devices.revoke.passwordRequired"));
      }

      const stepUp =
        lifecycleAction === "suspend"
          ? await issueBranchSuspendStepUp(organizationId, branchId, lifecyclePassword)
          : lifecycleAction === "archive"
            ? await issueBranchArchiveStepUp(organizationId, branchId, lifecyclePassword)
            : lifecycleAction === "reactivate"
              ? await issueBranchReactivateStepUp(organizationId, branchId, lifecyclePassword)
              : await issueBranchSetPrimaryStepUp(organizationId, branchId, lifecyclePassword);

      if (!stepUp.ok) {
        throw new Error(stepUpMessage(stepUp.reason, t));
      }

      const body = {
        reason: reasonRequired ? lifecycleReason.trim() : null,
        stepUpToken: stepUp.value.stepUpToken,
      };

      const result =
        lifecycleAction === "suspend"
          ? await suspendOrganizationBranch(organizationId, branchId, body)
          : lifecycleAction === "archive"
            ? await archiveOrganizationBranch(organizationId, branchId, body)
            : lifecycleAction === "reactivate"
              ? await reactivateOrganizationBranch(organizationId, branchId, body)
              : await setPrimaryOrganizationBranch(organizationId, branchId, body);

      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.saveFailed"));
      }
      return { action: lifecycleAction, branch: result.value };
    },
    onSuccess: async ({ action }) => {
      closeLifecycle();
      setConfirmPrimary(false);
      setDetailsMessage(
        action === "suspend"
          ? t("branches.detail.suspended")
          : action === "archive"
            ? t("branches.detail.archived")
            : action === "reactivate"
              ? t("branches.detail.reactivated")
              : t("branches.detail.setPrimaryDone"),
      );
      await queryClient.invalidateQueries({
        queryKey: ["branch-management-detail", organizationId, branchId],
      });
      await queryClient.invalidateQueries({
        queryKey: ["branch-management-summary", organizationId],
      });
    },
    onError: (error) => {
      setLifecycleError(error instanceof Error ? error.message : t("branches.saveFailed"));
    },
  });

  if (!canManage) {
    return (
      <div className="branch-mgmt-page exits-page flex min-w-0 flex-col gap-3" data-testid="branch-mgmt-detail-denied">
        <PageHeader
          title={t("branches.mgmt.title")}
          description={t("branches.mgmt.denied")}
          backTo="/org/branches"
          backLabel={t("branches.backList")}
          backTestId="page-header-back-branches"
        />
      </div>
    );
  }

  if (branchQuery.isLoading) {
    return <LoadingSkeleton count={4} label={t("loading.label")} />;
  }

  if (branchQuery.isError || !branch) {
    return (
      <div className="branch-mgmt-page exits-page flex min-w-0 flex-col gap-3">
        <PageHeader
          title={t("branches.mgmt.title")}
          description={t("branches.mgmt.lede")}
          backTo="/org/branches"
          backLabel={t("branches.backList")}
          backTestId="page-header-back-branches"
        />
        <ErrorState
          title={t("branches.notFound")}
          detail={
            branchQuery.error instanceof Error
              ? branchQuery.error.message
              : t("branches.mgmt.loadError")
          }
        />
      </div>
    );
  }

  const copy = branchAdminCopy(branch.branchType);
  const isWarehouse = copy.warehouse;
  const detailTabs = isWarehouse ? WAREHOUSE_DETAIL_TABS : RETAIL_DETAIL_TABS;
  const warehouseOps = [
    canInventory
      ? {
          id: "inventory",
          to: "/inventory",
          label: t("branches.detail.op.inventory"),
          testId: "branch-warehouse-op-inventory",
        }
      : null,
    canReceive
      ? {
          id: "receive",
          to: "/purchasing/receive-stock",
          label: t("branches.detail.op.receive"),
          testId: "branch-warehouse-op-receive",
        }
      : null,
    canInventory
      ? {
          id: "transfers",
          to: "/inventory/transfers",
          label: t("branches.detail.op.transfers"),
          testId: "branch-warehouse-op-transfers",
        }
      : null,
    canPurchasing
      ? {
          id: "purchasing",
          to: "/purchasing",
          label: t("branches.detail.op.purchasing"),
          testId: "branch-warehouse-op-purchasing",
        }
      : null,
  ].filter(Boolean) as Array<{ id: string; to: string; label: string; testId: string }>;

  return (
    <div
      className="branch-mgmt-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="branch-mgmt-detail"
      data-branch-type={isWarehouse ? "Warehouse" : "Retail"}
    >
      <PageHeader
        title={branch.name}
        description={t("branches.mgmt.lede")}
        backTo="/org/branches"
        backLabel={t("branches.backList")}
        backTestId="page-header-back-branches"
      />

      <div className="branch-mgmt-card__badges flex flex-wrap gap-2">
        <span data-testid="branch-detail-code">{branch.code}</span>
        <span data-testid="branch-detail-type-chip">
          <StatusChip tone={isWarehouse ? "warning" : "info"}>
            {isWarehouse ? t("branches.type.warehouse") : t("branches.type.retail")}
          </StatusChip>
        </span>
        {!isWarehouse && branch.isPrimary ? (
          <span data-testid="branch-detail-primary-badge">
            <StatusChip tone="info">{t("branches.mgmt.primary")}</StatusChip>
          </span>
        ) : null}
        {!isWarehouse && !branch.isPrimary ? (
          <StatusChip tone="info">{t("branches.mgmt.secondary")}</StatusChip>
        ) : null}
        <StatusChip
          tone={
            statusKind === "Active" ? "success" : statusKind === "Suspended" ? "warning" : "info"
          }
        >
          {statusLabel(branch.status, t)}
        </StatusChip>
      </div>

      <UnderlineTabBar
        ariaLabel={t("branches.setupTabsLabel")}
        testId="branch-mgmt-tabs"
        activeKey={activeTab}
        onChange={(key) => selectTab(parseDetailTab(key, isWarehouse))}
        items={detailTabs.map((tab) => ({
          key: tab,
          label:
            tab === "overview"
              ? t(copy.overviewTab)
              : tab === "details"
                ? t(copy.detailsTab)
                : tab === "staff"
                  ? t("branches.detail.staff")
                  : tab === "devices"
                    ? t("branches.detail.devices")
                    : t("branches.detail.fulfillment"),
          testId: `branch-mgmt-tab-${tab}`,
        }))}
      />

      {detailsMessage ? (
        <div className="exits-alert exits-alert--success" role="status" data-testid="branch-detail-message">
          <p className="m-0 text-[length:var(--exits-text-sm)]">{detailsMessage}</p>
        </div>
      ) : null}

      {activeTab === "overview" ? (
        <div className="flex flex-col gap-3" data-testid="branch-mgmt-overview">
          <section className="catalog-form-section exits-animate-panel gap-2">
            <h2 className="catalog-form-section__title">{t(copy.overviewTab)}</h2>
            <dl className="branch-mgmt-card__meta">
              <div>
                <dt>{t(copy.nameLabel)}</dt>
                <dd>{branch.name}</dd>
              </div>
              <div>
                <dt>{t(copy.codeLabel)}</dt>
                <dd>{branch.code}</dd>
              </div>
              <div>
                <dt>{t("areas.singular")}</dt>
                <dd data-testid="branch-detail-area">
                  {summary?.areaName ?? t("areas.unassigned")}
                </dd>
              </div>
              <div>
                <dt>{t("branches.mgmt.staffAccess")}</dt>
                <dd data-testid="branch-detail-staff-count">
                  {summary?.assignedStaffCount ?? "—"}
                </dd>
              </div>
              <div>
                <dt>{t(copy.devicesLabel)}</dt>
                <dd data-testid="branch-detail-device-count">
                  {t("branches.mgmt.devicesActive").replace(
                    "{count}",
                    String(summary?.activeDeviceCount ?? 0),
                  )}
                </dd>
              </div>
              {!isWarehouse ? (
                <>
                  <div>
                    <dt>{t("branches.mgmt.pickup")}</dt>
                    <dd>
                      {branch.pickupEnabled ? t("branches.mgmt.on") : t("branches.mgmt.off")} ·{" "}
                      {summary?.pickupSectionsComplete ?? 0}/{summary?.pickupSectionsTotal ?? 2}
                    </dd>
                  </div>
                  <div>
                    <dt>{t("branches.mgmt.delivery")}</dt>
                    <dd>
                      {branch.deliveryEnabled ? t("branches.mgmt.on") : t("branches.mgmt.off")} ·{" "}
                      {summary?.deliverySectionsComplete ?? 0}/{summary?.deliverySectionsTotal ?? 5}
                    </dd>
                  </div>
                </>
              ) : null}
            </dl>
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="outline" onClick={() => selectTab("details")}>
                {t(copy.detailsTab)}
              </Button>
              <Button type="button" variant="outline" onClick={() => selectTab("staff")}>
                {t("branches.detail.staff")}
              </Button>
              <Button type="button" variant="outline" onClick={() => selectTab("devices")}>
                {t("branches.detail.devices")}
              </Button>
              {!isWarehouse ? (
                <Button asChild variant="outline" data-testid="branch-mgmt-configure-fulfillment">
                  <Link to={branchFulfillmentEditPath(branch.id)}>
                    {t("branches.detail.configureFulfillment")}
                  </Link>
                </Button>
              ) : null}
            </div>
          </section>

          {isWarehouse && warehouseOps.length > 0 ? (
            <section
              className="catalog-form-section exits-animate-panel gap-2"
              data-testid="branch-warehouse-operations"
            >
              <h2 className="catalog-form-section__title">{t("branches.detail.operations")}</h2>
              <div className="flex flex-wrap gap-2">
                {warehouseOps.map((op) => (
                  <Button key={op.id} asChild variant="outline" data-testid={op.testId}>
                    <Link to={op.to}>
                      {op.label}
                      <span aria-hidden> →</span>
                    </Link>
                  </Button>
                ))}
              </div>
            </section>
          ) : null}

          {!isWarehouse && organizationId ? (
            <BranchStorefrontQrPanel
              organizationId={organizationId}
              organizationDisplayName={boundWorkspace?.organizationDisplayName ?? "store"}
              branchId={branch.id}
              branchName={branch.name}
              branchStatus={branch.status}
            />
          ) : null}

          {canGovern ? (
            <section className="catalog-form-section exits-animate-panel gap-2" data-testid="branch-lifecycle">
              <h2 className="catalog-form-section__title">{t(copy.lifecycleTitle)}</h2>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("branches.detail.historyKept")}
              </p>
              <div className="flex flex-wrap gap-2">
                {!isWarehouse && !branch.isPrimary && statusKind === "Active" ? (
                  <Button
                    type="button"
                    variant="outline"
                    data-testid="branch-make-primary"
                    onClick={() => setConfirmPrimary(true)}
                  >
                    {t("branches.detail.makePrimary")}
                  </Button>
                ) : null}
                {!branch.isPrimary && statusKind === "Active" ? (
                  <Button
                    type="button"
                    variant="outline"
                    data-testid="branch-suspend"
                    onClick={() => setLifecycleAction("suspend")}
                  >
                    {t("branches.detail.suspend")}
                  </Button>
                ) : null}
                {statusKind === "Suspended" ? (
                  <Button
                    type="button"
                    variant="outline"
                    data-testid="branch-reactivate"
                    onClick={() => setLifecycleAction("reactivate")}
                  >
                    {t("branches.detail.reactivate")}
                  </Button>
                ) : null}
                {!branch.isPrimary && statusKind !== "Archived" ? (
                  <Button
                    type="button"
                    variant="outline"
                    data-testid="branch-archive"
                    onClick={() => setLifecycleAction("archive")}
                  >
                    {t("branches.detail.archive")}
                  </Button>
                ) : null}
              </div>
            </section>
          ) : null}
        </div>
      ) : null}

      {activeTab === "details" ? (
        <div className="flex flex-col gap-3" data-testid="branch-mgmt-details">
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t(copy.codeLabel)}
            <input
              className="catalog-form-select bg-[var(--exits-surface-muted)] font-normal"
              value={branch.code}
              readOnly
              aria-readonly="true"
              data-testid="branch-detail-code-input"
            />
          </label>
          <BranchDetailsForm
            name={detailsDraft.name}
            contactPhone={detailsDraft.contactPhone}
            addressLine1={detailsDraft.addressLine1}
            addressLine2={detailsDraft.addressLine2}
            city={detailsDraft.city}
            region={detailsDraft.region}
            postalCode={detailsDraft.postalCode}
            branchType={detailsDraft.branchType}
            warehouseAllowed={warehouseAllowed}
            t={t}
            onChange={(field, value) =>
              setDetailsDraft((prev) => ({
                ...prev,
                [field]:
                  field === "branchType"
                    ? value === "Warehouse"
                      ? "Warehouse"
                      : "Retail"
                    : value,
              }))
            }
          />
          {detailsError ? (
            <div className="exits-alert exits-alert--error" role="alert">
              <p className="m-0 text-[length:var(--exits-text-sm)]">{detailsError}</p>
            </div>
          ) : null}
          <Button
            type="button"
            className="self-start"
            data-testid="branch-details-save"
            disabled={saveDetailsMutation.isPending}
            onClick={() => saveDetailsMutation.mutate()}
          >
            {saveDetailsMutation.isPending ? t("branches.saving") : t("branches.saveDetails")}
          </Button>
        </div>
      ) : null}

      {activeTab === "staff" && organizationId ? (
        <BranchStaffAccessPanel
          organizationId={organizationId}
          branchId={branchId}
          sessionGrant={sessionGrant}
        />
      ) : null}

      {activeTab === "devices" ? (
        <div className="flex flex-col gap-3" data-testid="branch-devices-panel">
          {devicesQuery.isLoading ? <LoadingSkeleton label={t("loading.label")} /> : null}
          {devicesQuery.isError ? (
            <ErrorState title={t("error.title")} detail={t("devices.loadError")} />
          ) : null}
          {devicesQuery.isSuccess && branchDevices.length === 0 ? (
            <EmptyState title={t("branches.devices.empty")} detail="" />
          ) : null}
          {branchDevices.length > 0 ? (
            <ul className="exits-list m-0 grid list-none gap-2 p-0" data-testid="branch-devices-list">
              {branchDevices.map((device) => (
                <li key={device.id}>
                  <article className="exits-list__card device-row min-w-0">
                    <div className="device-row__main min-w-0">
                      <p className="exits-list__name m-0 truncate font-semibold">
                        {device.friendlyName}
                      </p>
                      <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                        {device.status}
                      </p>
                    </div>
                  </article>
                </li>
              ))}
            </ul>
          ) : null}
          <Button asChild variant="outline" className="self-start" data-testid="branch-devices-manage">
            <Link to="/org/devices">{t("branches.devices.manage")}</Link>
          </Button>
        </div>
      ) : null}

      {activeTab === "fulfillment" && !isWarehouse ? (
        <div className="flex flex-col gap-3" data-testid="branch-fulfillment-summary">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("branches.detail.fulfillmentLede")}
          </p>
          <dl className="branch-mgmt-card__meta">
            <div>
              <dt>{t("branches.mgmt.pickup")}</dt>
              <dd>
                {branch.pickupEnabled ? t("branches.mgmt.on") : t("branches.mgmt.off")} ·{" "}
                {summary?.pickupSectionsComplete ?? 0}/{summary?.pickupSectionsTotal ?? 2}
              </dd>
            </div>
            <div>
              <dt>{t("branches.mgmt.delivery")}</dt>
              <dd>
                {branch.deliveryEnabled ? t("branches.mgmt.on") : t("branches.mgmt.off")} ·{" "}
                {summary?.deliverySectionsComplete ?? 0}/{summary?.deliverySectionsTotal ?? 5}
              </dd>
            </div>
          </dl>
          <Button asChild className="self-start" data-testid="branch-fulfillment-configure">
            <Link to={branchFulfillmentEditPath(branch.id)}>
              {t("branches.detail.configureFulfillment")}
            </Link>
          </Button>
        </div>
      ) : null}

      <ConfirmationDialog
        open={confirmPrimary}
        onCancel={() => setConfirmPrimary(false)}
        title={t("branches.detail.changePrimary")}
        detail={t("branches.detail.changePrimaryConfirm").replace("{name}", branch.name)}
        confirmLabel={t("branches.detail.makePrimary")}
        cancelLabel={t("branches.cancel")}
        onConfirm={() => {
          setConfirmPrimary(false);
          setLifecycleAction("set-primary");
        }}
        testId="branch-primary-confirm"
      />

      <BottomSheet
        open={lifecycleAction !== null}
        onClose={closeLifecycle}
        panelId="branch-lifecycle-panel"
        testId="branch-lifecycle-panel"
        title={
          lifecycleAction === "suspend"
            ? t("branches.detail.suspend")
            : lifecycleAction === "archive"
              ? t("branches.detail.archive")
              : lifecycleAction === "reactivate"
                ? t("branches.detail.reactivate")
                : t("branches.detail.makePrimary")
        }
        closeLabel={t("branches.cancel")}
        presentation="sheet-mobile-dialog-desktop"
      >
        <div className="flex flex-col gap-3">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("branches.detail.historyKept")}
          </p>
          {reasonRequired ? (
            <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
              {t("branches.detail.reason")}
              <textarea
                className="catalog-form-select min-h-24 font-normal"
                value={lifecycleReason}
                onChange={(e) => setLifecycleReason(e.target.value)}
                data-testid="branch-lifecycle-reason"
              />
            </label>
          ) : null}
          <label className="flex flex-col gap-1.5 text-[length:var(--exits-text-sm)] font-semibold">
            {t("devices.revoke.passwordLabel")}
            <span className="relative">
              <input
                className="catalog-form-select w-full pr-11 font-normal"
                type={lifecyclePasswordVisible ? "text" : "password"}
                value={lifecyclePassword}
                onChange={(e) => setLifecyclePassword(e.target.value)}
                autoComplete="current-password"
                data-testid="branch-lifecycle-password"
              />
              <button
                type="button"
                className="absolute right-2 top-1/2 -translate-y-1/2 rounded p-2 text-muted"
                onClick={() => setLifecyclePasswordVisible((v) => !v)}
                aria-label={
                  lifecyclePasswordVisible
                    ? t("devices.revoke.hidePassword")
                    : t("devices.revoke.showPassword")
                }
              >
                {lifecyclePasswordVisible ? (
                  <EyeOff className="size-4" aria-hidden />
                ) : (
                  <Eye className="size-4" aria-hidden />
                )}
              </button>
            </span>
          </label>
          {lifecycleError ? (
            <div className="exits-alert exits-alert--error" role="alert" data-testid="branch-lifecycle-error">
              <div className="flex gap-3">
                <CircleAlert className="mt-0.5 size-5 shrink-0 text-destructive" aria-hidden />
                <p className="m-0 text-[length:var(--exits-text-sm)] text-destructive">{lifecycleError}</p>
              </div>
            </div>
          ) : null}
          <Button
            type="button"
            data-testid="branch-lifecycle-confirm"
            disabled={
              lifecycleMutation.isPending ||
              !lifecyclePassword.trim() ||
              (reasonRequired && lifecycleReason.trim().length < MIN_REASON_LENGTH)
            }
            onClick={() => lifecycleMutation.mutate()}
          >
            {lifecycleMutation.isPending ? t("branches.saving") : t("branches.save")}
          </Button>
        </div>
      </BottomSheet>
    </div>
  );
}
