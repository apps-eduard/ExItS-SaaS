import { useEffect, useState } from "react";
import { useParams, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  Ban,
  IdCard,
  RotateCcw,
  Store,
  Wallet,
} from "lucide-react";
import {
  canApproveCustomerCreditPolicy,
  canEditCustomer,
  canManageCustomerBranchAccess,
  canManageCustomerCreditPolicy,
  canRecordRepayment,
  canViewStatement,
} from "@/access/pos-capabilities";
import {
  createCustomerLinkRequestForCustomer,
  getCustomerLinkStatus,
  listCustomerLinkRequestHistory,
  remindCustomerLinkRequest,
  revokeCustomerLinkRequest,
} from "@/api/platform/customer-link-status-client";
import {
  getOrganizationBusinessCustomer,
  updateBusinessCustomerDeliveryPreferences,
} from "@/api/platform/business-customer-delivery-client";
import { getLinkedCustomerPersonalProfile } from "@/api/platform/linked-customer-profile-client";
import { resolvePublicUserId } from "@/api/platform/public-identity-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import {
  deactivateCustomer,
  getCustomer,
  getCustomerCreditSummary,
  reactivateCustomer,
  type PosCustomerListItem,
} from "@/api/pos/pos-customers-client";
import { CustomerDeliveryExceptionSection } from "@/features/customers/CustomerDeliveryExceptionSection";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { ExitsTabs } from "@/components/exits/ExitsTabs";
import { PageHeader } from "@/components/exits/PageHeader";
import { pageBackNav } from "@/navigation/page-back-nav";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  customerLinkStatusLabelKey,
  extractPersonalExItsIdFromNotes,
  mapPlatformCustomerLinkStatus,
  resolveDisplayedPersonalExItsId,
  type CustomerLinkUiStatus,
} from "@/features/customers/customer-link-status";
import { mapOrgLinkStatusToRelationship } from "@/features/customer-connection/connection-state";
import { initialsFor } from "@/features/personal/people-status";
import { profilePhotoSrc } from "@/features/personal/profile-photo";
import { CustomerPersonalLinkSection } from "@/features/customers/CustomerPersonalLinkSection";
import { CreditPolicySection } from "@/features/customers/CreditPolicySection";
import { CustomerOnlineOrderingAccessSection } from "@/features/customers/CustomerOnlineOrderingAccessSection";
import { CustomerBranchVisibilitySection } from "@/features/customers/CustomerBranchVisibilitySection";
import { PaymentHistorySection } from "@/features/customers/PaymentHistorySection";
import { RecordPaymentModal } from "@/features/customers/RecordPaymentModal";
import { useI18n } from "@/i18n/I18nProvider";
import {
  cacheCustomer,
  getCachedCustomer,
} from "@/offline/customer-cache";
import { onlineRequiredDetailKey, ONLINE_REQUIRED_CODES } from "@/offline/online-required";
import { useOrganizationOfflineContext } from "@/offline/organization-offline-context";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

const blankValue = "—";

function genderLabel(value: string | null | undefined, t: (key: "personal.profile.genderMale" | "personal.profile.genderFemale" | "personal.profile.genderOther") => string): string {
  const trimmed = value?.trim() ?? "";
  if (trimmed === "Male") return t("personal.profile.genderMale");
  if (trimmed === "Female") return t("personal.profile.genderFemale");
  if (trimmed === "Other") return t("personal.profile.genderOther");
  return trimmed;
}

function splitStoredAddress(address: string | null | undefined): {
  addressLine1: string;
  city: string;
  province: string;
  postal: string;
} {
  const parts = (address ?? "")
    .split(",")
    .map((part) => part.trim())
    .filter(Boolean);
  if (parts.length <= 1) {
    return { addressLine1: parts[0] ?? "", city: "", province: "", postal: "" };
  }
  return {
    addressLine1: parts[0] ?? "",
    city: parts[1] ?? "",
    province: parts[2] ?? "",
    postal: parts.slice(3).join(", "),
  };
}

function CustomerHeaderPhoto({ url, name }: { url: string; name: string }) {
  const [failed, setFailed] = useState(false);
  useEffect(() => {
    setFailed(false);
  }, [url]);
  const photoUrl = profilePhotoSrc(url);
  const src = failed ? null : photoUrl;
  const initials = initialsFor(name);
  return (
    <span
      className="personal-profile-header__photo personal-profile-header__photo--before-name"
      data-testid="customer-header-photo"
      data-photo-url={photoUrl ?? ""}
    >
      {src ? (
        <img src={src} alt="" onError={() => setFailed(true)} />
      ) : initials === "?" ? (
        blankValue
      ) : (
        initials
      )}
    </span>
  );
}

function ReadOnlyField({
  label,
  value,
  testId,
}: {
  label: string;
  value: string;
  testId?: string;
}) {
  return (
    <div className="flex min-w-0 flex-col gap-1 text-sm">
      <span className="font-semibold">{label}</span>
      <span className="whitespace-pre-wrap" data-testid={testId}>
        {value.trim() || blankValue}
      </span>
    </div>
  );
}

export function CustomerDetailPage() {
  const { t } = useI18n();
  const { customerId } = useParams<{ customerId: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const pendingLinkHint = searchParams.get("pendingLink") === "1";
  const { sessionGrant } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const offlineContext = useOrganizationOfflineContext();
  const smartBack = usePageSmartBack({
    fallback: "customers",
    backLabel: t(pageBackNav.customers.labelKey),
    backTestId: "page-header-back-customers",
  });
  const [actionError, setActionError] = useState<string | null>(null);
  const [acting, setActing] = useState(false);
  const [recordPaymentOpen, setRecordPaymentOpen] = useState(false);
  const [afterCreateHintDismissed, setAfterCreateHintDismissed] = useState(false);
  const [detailTab, setDetailTab] = useState("details");
  const [cachedCustomer, setCachedCustomer] = useState<PosCustomerListItem | null>(null);

  const allowEdit = canEditCustomer(sessionGrant);
  const allowRepay = canRecordRepayment(sessionGrant);
  const allowStatement = canViewStatement(sessionGrant);
  const allowManageCreditPolicy = canManageCustomerCreditPolicy(sessionGrant);
  const allowApproveCreditPolicy = canApproveCustomerCreditPolicy(sessionGrant);
  const allowManageBranchAccess = canManageCustomerBranchAccess(sessionGrant);

  const enabledOnline = Boolean(workspace) && Boolean(customerId) && online;

  useEffect(() => {
    if (searchParams.get("recordPayment") !== "1") {
      return;
    }
    setRecordPaymentOpen(true);
    const next = new URLSearchParams(searchParams);
    next.delete("recordPayment");
    setSearchParams(next, { replace: true });
  }, [searchParams, setSearchParams]);

  const customerQuery = useQuery({
    queryKey: ["customers", "detail", workspace?.organizationId, customerId],
    enabled: enabledOnline,
    queryFn: ({ signal }) => getCustomer(workspace!, customerId!, signal),
  });

  const creditSummaryQuery = useQuery({
    queryKey: ["customers", "credit-summary", workspace?.organizationId, customerId],
    enabled: enabledOnline,
    queryFn: ({ signal }) => getCustomerCreditSummary(workspace!, customerId!, signal),
  });

  const platformCustomerId = customerQuery.data?.platformBusinessCustomerId ?? null;
  const linkStatusQuery = useQuery({
    queryKey: [
      "customers",
      "platform-link-status",
      workspace?.organizationId,
      platformCustomerId,
    ],
    enabled: enabledOnline && Boolean(platformCustomerId),
    queryFn: ({ signal }) =>
      getCustomerLinkStatus(workspace!.organizationId, platformCustomerId!, signal),
    refetchOnWindowFocus: true,
  });

  const linkHistoryQuery = useQuery({
    queryKey: [
      "customers",
      "platform-link-history",
      workspace?.organizationId,
      platformCustomerId,
    ],
    enabled: enabledOnline && Boolean(platformCustomerId),
    queryFn: ({ signal }) =>
      listCustomerLinkRequestHistory(workspace!.organizationId, platformCustomerId!, signal),
  });

  const deliveryPrefsQuery = useQuery({
    queryKey: [
      "customers",
      "delivery-preferences",
      workspace?.organizationId,
      platformCustomerId,
    ],
    enabled: enabledOnline && Boolean(platformCustomerId),
    queryFn: ({ signal }) =>
      getOrganizationBusinessCustomer(workspace!.organizationId, platformCustomerId!, signal),
  });

  const personalExItsIdForProfile = customerQuery.data
    ? resolveDisplayedPersonalExItsId({
        linkedPersonalPublicUserId: customerQuery.data.linkedPersonalPublicUserId,
        notes: customerQuery.data.notes,
      })
    : null;

  const personalProfileQuery = useQuery({
    queryKey: ["customers", "personal-profile", personalExItsIdForProfile],
    enabled: enabledOnline && Boolean(personalExItsIdForProfile),
    queryFn: ({ signal }) =>
      resolvePublicUserId(personalExItsIdForProfile!, "SaleCustomer", signal),
  });

  const linkedPersonalUserId =
    linkStatusQuery.data != null &&
    mapPlatformCustomerLinkStatus(linkStatusQuery.data.status) === "Linked"
      ? linkStatusQuery.data.linkedUserIdentityId
      : null;
  const linkedProfileQuery = useQuery({
    queryKey: ["customers", "linked-personal-profile", workspace?.organizationId, linkedPersonalUserId],
    enabled: enabledOnline && Boolean(workspace?.organizationId) && Boolean(linkedPersonalUserId),
    queryFn: ({ signal }) =>
      getLinkedCustomerPersonalProfile(workspace!.organizationId, linkedPersonalUserId!, signal),
  });

  const deliveryExceptionMutation = useMutation({
    mutationFn: async (next: boolean) => {
      if (!workspace || !platformCustomerId) {
        throw new Error("missing-customer");
      }
      return updateBusinessCustomerDeliveryPreferences(
        workspace.organizationId,
        platformCustomerId,
        next,
      );
    },
    onSuccess: async () => {
      setActionError(null);
      await queryClient.invalidateQueries({
        queryKey: [
          "customers",
          "delivery-preferences",
          workspace?.organizationId,
          platformCustomerId,
        ],
      });
      await queryClient.invalidateQueries({
        queryKey: ["customers", "delivery-exception-ids", workspace?.organizationId],
      });
    },
    onError: (error) => {
      setActionError(
        error instanceof PlatformApiError
          ? (error.problem.detail ?? t("customers.delivery.updateFailed"))
          : t("customers.delivery.updateFailed"),
      );
    },
  });

  async function invalidateLinkQueries() {
    await Promise.all([
      queryClient.invalidateQueries({
        queryKey: ["customers", "platform-link-status", workspace?.organizationId, platformCustomerId],
      }),
      queryClient.invalidateQueries({
        queryKey: ["customers", "platform-link-history", workspace?.organizationId, platformCustomerId],
      }),
    ]);
  }

  const remindMutation = useMutation({
    mutationFn: async () => {
      const requestId = linkStatusQuery.data?.latestLinkRequestId;
      if (!workspace || !requestId) {
        throw new Error("missing-request");
      }
      return remindCustomerLinkRequest(workspace.organizationId, requestId);
    },
    onSuccess: async () => {
      setActionError(null);
      await invalidateLinkQueries();
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("error.detail"));
    },
  });

  const revokeMutation = useMutation({
    mutationFn: async () => {
      const requestId = linkStatusQuery.data?.latestLinkRequestId;
      if (!workspace || !requestId) {
        throw new Error("missing-request");
      }
      await revokeCustomerLinkRequest(workspace.organizationId, requestId);
    },
    onSuccess: async () => {
      setActionError(null);
      await invalidateLinkQueries();
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("error.detail"));
    },
  });

  const inviteAgainMutation = useMutation({
    mutationFn: async () => {
      const row = customerQuery.data;
      if (!workspace || !row?.platformBusinessCustomerId) {
        throw new Error("missing-customer");
      }
      const publicUserId =
        row.linkedPersonalPublicUserId?.trim() ||
        extractPersonalExItsIdFromNotes(row.notes).exItsId ||
        null;
      await createCustomerLinkRequestForCustomer({
        organizationId: workspace.organizationId,
        platformBusinessCustomerId: row.platformBusinessCustomerId,
        publicUserId,
      });
    },
    onSuccess: async () => {
      setActionError(null);
      await invalidateLinkQueries();
    },
    onError: (error) => {
      setActionError(error instanceof Error ? error.message : t("error.detail"));
    },
  });

  useEffect(() => {
    if (!offlineContext || !online) {
      return;
    }
    if (customerQuery.data) {
      void cacheCustomer(offlineContext.db, offlineContext.scopeBinding, customerQuery.data).catch(
        () => {},
      );
    }
  }, [customerQuery.data, offlineContext, online]);

  useEffect(() => {
    if (!offlineContext || online || !customerId) {
      return;
    }
    let cancelled = false;
    void getCachedCustomer(offlineContext.db, offlineContext.scopeBinding, customerId).then(
      (cachedRow) => {
        if (cancelled) {
          return;
        }
        setCachedCustomer(cachedRow);
      },
    );
    return () => {
      cancelled = true;
    };
  }, [customerId, offlineContext, online]);

  if (!workspace || !customerId) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (customerQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  const customer = customerQuery.data ?? cachedCustomer;

  if (!customer) {
    return (
      <ErrorState
        title={t("error.title")}
        detail={
          online
            ? ((customerQuery.error as Error | undefined)?.message ?? t("customers.notFound"))
            : t("offline.customerNotCached")
        }
      />
    );
  }

  const usingCachedCustomer = !customerQuery.data;
  const isActive = customer.status.toLowerCase() === "active";

  const linkUiStatus: CustomerLinkUiStatus = (() => {
    if (!customer.platformBusinessCustomerId?.trim()) {
      return "NotLinked";
    }
    if (!online) {
      return "Unavailable";
    }
    if (linkStatusQuery.isError) {
      return "Unavailable";
    }
    if (linkStatusQuery.data) {
      return mapPlatformCustomerLinkStatus(linkStatusQuery.data.status);
    }
    // Authoritative Platform status still loading — never invent Linked.
    return "Unavailable";
  })();

  const isLinked = linkUiStatus === "Linked";

  const showAfterCreateHint =
    pendingLinkHint &&
    !afterCreateHintDismissed &&
    (linkUiStatus === "Pending" || (linkStatusQuery.isLoading && !linkStatusQuery.data));
  const linkData = linkStatusQuery.data;
  const showUnavailableBanner =
    online &&
    linkData !== undefined &&
    mapPlatformCustomerLinkStatus(linkData.status) === "Unavailable";
  const linkMeta = linkData;
  const reminderCooldownActive =
    linkUiStatus === "Pending" &&
    Boolean(linkMeta?.nextReminderEligibleAtUtc) &&
    new Date(linkMeta!.nextReminderEligibleAtUtc!).getTime() > Date.now();

  const linkHistoryItems = (linkHistoryQuery.data ?? []).slice(0, 8);
  const linkedAtUtc =
    linkHistoryItems.find((item) => mapOrgLinkStatusToRelationship(item.status) === "Linked")
      ?.createdAtUtc ??
    linkHistoryItems[0]?.createdAtUtc ??
    linkMeta?.invitationSentAtUtc ??
    null;

  const personalExItsId = resolveDisplayedPersonalExItsId({
    linkedPersonalPublicUserId: customer.linkedPersonalPublicUserId,
    notes: customer.notes,
  });
  const linkedProfile = linkedProfileQuery.data;
  const linkedDisplayName =
    linkedProfile?.displayName?.trim() ||
    [linkedProfile?.firstName, linkedProfile?.lastName].filter((part) => part?.trim()).join(" ");
  const headerTitle =
    (isLinked ? linkedDisplayName || personalProfileQuery.data?.displayName?.trim() : null) ||
    customer.displayName;

  async function toggleStatus() {
    if (!allowEdit || acting || !workspace || !customerId) {
      return;
    }
    if (!online) {
      setActionError(t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.CustomerStatus)));
      return;
    }
    if (isActive && !window.confirm(t("customers.deactivateConfirm"))) {
      return;
    }
    setActing(true);
    setActionError(null);
    try {
      if (isActive) {
        await deactivateCustomer(workspace, customerId);
      } else {
        await reactivateCustomer(workspace, customerId);
      }
      await queryClient.invalidateQueries({ queryKey: ["customers"] });
    } catch (err) {
      setActionError(err instanceof Error ? err.message : t("error.detail"));
    } finally {
      setActing(false);
    }
  }

  const statusToggleButton = allowEdit ? (
    <Button
      type="button"
      variant={isActive ? "destructive" : "success"}
      data-testid="customer-toggle-status"
      disabled={acting}
      onClick={() => void toggleStatus()}
    >
      {isActive ? (
        <Ban className="size-4 shrink-0" aria-hidden />
      ) : (
        <RotateCcw className="size-4 shrink-0" aria-hidden />
      )}
      {isActive ? t("customers.deactivate") : t("customers.reactivate")}
    </Button>
  ) : null;

  const deliveryCard =
    platformCustomerId && online ? (
      <CustomerDeliveryExceptionSection
        allowBeyond={deliveryPrefsQuery.data?.allowDeliveryBeyondNormalDistance ?? false}
        canEdit={allowEdit}
        pending={deliveryExceptionMutation.isPending}
        t={t}
        onToggle={(next) => deliveryExceptionMutation.mutate(next)}
      />
    ) : null;

  const storedAddress = splitStoredAddress(customer.address);
  const detailName =
    (isLinked ? linkedDisplayName : null) ||
    personalProfileQuery.data?.displayName?.trim() ||
    customer.displayName ||
    blankValue;
  const detailEmail = linkedProfile?.email?.trim()
    ? linkedProfile.email.trim()
    : personalProfileQuery.isLoading
      ? "…"
      : personalProfileQuery.data?.maskedEmail?.trim() || blankValue;
  const detailMobile = linkedProfile?.mobileNumber?.trim() || customer.mobileNumber || "";
  const detailAddress = linkedProfile
    ? {
        country: linkedProfile.country ?? "",
        province: linkedProfile.provinceState ?? "",
        city: linkedProfile.cityMunicipality ?? "",
        barangay: linkedProfile.barangay ?? "",
        addressLine1: linkedProfile.addressLine1 ?? "",
        addressLine2: linkedProfile.addressLine2 ?? "",
        postal: linkedProfile.postalCode ?? "",
      }
    : {
        country: "",
        province: storedAddress.province,
        city: storedAddress.city,
        barangay: "",
        addressLine1: storedAddress.addressLine1,
        addressLine2: "",
        postal: storedAddress.postal,
      };
  const linkStatusText =
    online && !customer.platformBusinessCustomerId?.trim()
      ? t("customers.linkStatus.notLinked")
      : !online && customer.platformBusinessCustomerId?.trim()
        ? t("customers.linkStatus.unavailableOffline")
        : t(customerLinkStatusLabelKey(linkUiStatus));

  const headerPhoto = isLinked ? linkedProfile?.profilePhotoUrl?.trim() ?? "" : "";

  return (
    <div className="exits-page flex min-w-0 flex-col gap-4" data-testid="customer-detail-page">
      <PageHeader
        title={headerTitle}
        titleLeading={<CustomerHeaderPhoto url={headerPhoto} name={headerTitle} />}
        description={t("customers.detailLede")}
        {...smartBack}
      />
      {!isActive ? (
        <div className="flex flex-wrap items-center gap-2" data-testid="customer-deactivated-notice">
          <span data-testid="customer-account-status">
            <StatusChip tone="warning">{t("customers.statusInactive")}</StatusChip>
          </span>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.deactivatedNotice")}
          </p>
          {statusToggleButton}
        </div>
      ) : null}

      {usingCachedCustomer ? (
        <Card data-testid="customer-detail-cached-notice">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("offline.cachedBalanceNotice")}
          </p>
        </Card>
      ) : null}

      {!online && customer.platformBusinessCustomerId?.trim() ? (
        <Card data-testid="customer-link-status-offline">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.linkStatus.unavailableOffline")}
          </p>
        </Card>
      ) : null}

      {actionError ? (
        <Card>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {actionError}
          </p>
        </Card>
      ) : null}

      <ExitsTabs
        variant="underline"
        scrollable
        ariaLabel={t("customers.detailsTab")}
        testId="customer-detail-tabs"
        value={detailTab}
        onValueChange={setDetailTab}
        items={[
          {
            key: "details",
            label: t("customers.detailsTab"),
            icon: IdCard,
            testId: "customer-tab-details",
          },
          {
            key: "store",
            label: t("customers.storeSettings"),
            icon: Store,
            testId: "customer-tab-store",
          },
          {
            key: "credit",
            label: t("customers.creditPaymentTab"),
            icon: Wallet,
            testId: "customer-tab-credit",
          },
        ]}
        panels={{
          details: (
            <div className="flex flex-col gap-4">
              {linkUiStatus !== "NotLinked" ? (
                <CustomerPersonalLinkSection
                  linkUiStatus={linkUiStatus}
                  linkMeta={linkMeta}
                  showAfterCreateHint={showAfterCreateHint}
                  afterCreateHintDismissed={afterCreateHintDismissed}
                  onDismissAfterCreateHint={() => setAfterCreateHintDismissed(true)}
                  online={online}
                  allowEdit={allowEdit}
                  reminderCooldownActive={reminderCooldownActive}
                  remindPending={remindMutation.isPending}
                  revokePending={revokeMutation.isPending}
                  onRemind={() => remindMutation.mutate()}
                  onRevoke={() => revokeMutation.mutate()}
                />
              ) : null}

              {showUnavailableBanner ? (
                <Card data-testid="customer-link-unavailable-banner">
                  <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                    {t("customers.linkStatus.unavailable")}
                  </p>
                  <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {t("customers.linkConnectionUnavailableDetail")}
                  </p>
                </Card>
              ) : null}

              {(linkUiStatus === "Declined" ||
                linkUiStatus === "Expired" ||
                linkUiStatus === "Revoked") &&
              online &&
              allowEdit &&
              customer.platformBusinessCustomerId &&
              (customer.linkedPersonalPublicUserId ||
                extractPersonalExItsIdFromNotes(customer.notes).exItsId) ? (
                <Card data-testid="customer-link-invite-again-card">
                  <Button
                    type="button"
                    data-testid="customer-link-invite-again"
                    disabled={inviteAgainMutation.isPending}
                    onClick={() => inviteAgainMutation.mutate()}
                  >
                    {linkUiStatus === "Expired"
                      ? t("customers.linkSendNewInvite")
                      : t("customers.linkInviteAgain")}
                  </Button>
                </Card>
              ) : null}

              <div className="personal-profile-layout">
                <section
                  className="catalog-form-section personal-profile-section personal-profile-section--details personal-profile-section--wide flex flex-col gap-3"
                  data-testid={isLinked ? "customer-personal-profile" : "customer-details"}
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <h2 className="catalog-form-section__title m-0">{t("customers.detailsTab")}</h2>
                    {isActive ? statusToggleButton : null}
                  </div>
                  <div className="personal-profile-fields">
                    <ReadOnlyField
                      label={t("customers.displayName")}
                      value={detailName}
                      testId="customer-personal-profile-name"
                    />
                    <ReadOnlyField
                      label={t("customers.exItsIdLabel")}
                      value={personalExItsId ?? t("customers.exItsIdNone")}
                      testId="customer-exits-id"
                    />
                    <ReadOnlyField
                      label={t("customers.email")}
                      value={detailEmail}
                      testId="customer-personal-profile-email"
                    />
                    <ReadOnlyField
                      label={t("customers.mobile")}
                      value={detailMobile}
                      testId="customer-details-mobile"
                    />
                    <ReadOnlyField
                      label={t("personal.profile.gender")}
                      value={genderLabel(isLinked ? linkedProfile?.gender : null, t)}
                      testId="customer-details-gender"
                    />
                    {isLinked ? (
                      <div data-testid="customer-personal-profile-link-row">
                        <ReadOnlyField
                          label={t("customers.connectedSince")}
                          value={linkedAtUtc ? new Date(linkedAtUtc).toLocaleString() : blankValue}
                          testId="customer-personal-profile-connected-since"
                        />
                      </div>
                    ) : (
                      <ReadOnlyField
                        label={t("customers.linkStatusLabel")}
                        value={linkStatusText}
                        testId="customer-link-status-label"
                      />
                    )}
                  </div>
                </section>
                <section
                  className="catalog-form-section personal-profile-section personal-profile-section--address personal-profile-section--wide flex flex-col gap-3"
                  data-testid="customer-details-address"
                >
                  <h2 className="catalog-form-section__title">{t("staffBusinessProfile.sectionAddress")}</h2>
                  <div className="personal-profile-fields">
                    <ReadOnlyField label={t("personal.profile.country")} value={detailAddress.country} />
                    <ReadOnlyField label={t("personal.profile.province")} value={detailAddress.province} />
                    <ReadOnlyField label={t("personal.profile.city")} value={detailAddress.city} />
                    <ReadOnlyField label={t("personal.profile.barangay")} value={detailAddress.barangay} />
                    <ReadOnlyField
                      label={t("personal.profile.address1")}
                      value={detailAddress.addressLine1}
                      testId="customer-details-address-line"
                    />
                    <ReadOnlyField label={t("personal.profile.address2")} value={detailAddress.addressLine2} />
                    <ReadOnlyField label={t("personal.profile.postal")} value={detailAddress.postal} />
                  </div>
                </section>
              </div>
            </div>
          ),
          store: (
            <div className="flex flex-col gap-4">
              {deliveryCard ? (
                <Card className="p-4" data-testid="customer-delivery-card">
                  <dl className="branch-mgmt-overview__grid">{deliveryCard}</dl>
                </Card>
              ) : null}

              {workspace && customer && platformCustomerId ? (
                <CustomerOnlineOrderingAccessSection
                  workspace={workspace}
                  customer={customer}
                  canEdit={allowEdit}
                  online={online}
                />
              ) : null}

              {workspace && customerId ? (
                <CustomerBranchVisibilitySection
                  workspace={workspace}
                  organizationId={workspace.organizationId}
                  customerId={customerId}
                  online={online}
                  canManage={allowManageBranchAccess}
                />
              ) : null}
            </div>
          ),
          credit: (
            <div className="flex flex-col gap-4">
              {workspace && customerId ? (
                <CreditPolicySection
                  workspace={workspace}
                  customerId={customerId}
                  online={online}
                  canManage={allowManageCreditPolicy}
                  canApprove={allowApproveCreditPolicy}
                  canRecordPayment={allowRepay}
                  canViewStatement={allowStatement}
                  subjectIdentity={[headerTitle, personalExItsId]
                    .filter((part): part is string => Boolean(part))
                    .join(" · ")}
                  onRecordPayment={() => setRecordPaymentOpen(true)}
                />
              ) : null}

              {workspace && customerId ? (
                <PaymentHistorySection
                  customerKind="personal"
                  customerId={customerId}
                  online={online}
                  canManageChecks={allowRepay}
                />
              ) : null}
            </div>
          ),
        }}
      />

      {workspace && customerId ? (
        <RecordPaymentModal
          open={recordPaymentOpen}
          onOpenChange={setRecordPaymentOpen}
          customerKind="personal"
          customerId={customerId}
          displayName={headerTitle}
          outstandingBalance={creditSummaryQuery.data?.outstandingAmount ?? 0}
          onSuccess={() => {
            void queryClient.invalidateQueries({
              queryKey: ["customers", "credit-summary", workspace.organizationId, customerId],
            });
          }}
        />
      ) : null}
    </div>
  );
}
