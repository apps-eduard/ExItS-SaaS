import {
  canApproveCustomerCreditPolicy,
  canManageCustomerBranchAccess,
  canManageCustomerCreditPolicy,
  canManageSuppliers,
  canRecordRepayment,
  canViewStatement,
} from "@/access/pos-capabilities";
import {
  cancelConnectionRequest,
  getBusinessCustomer,
  getBusinessCustomerUtangSummary,
  getSupplierConnectedSupplierCommerceReadiness,
} from "@/api/pos/pos-connected-suppliers-client";
import {
  formatPublicBusinessAddress,
  getOrganizationB2bPublicProfile,
} from "@/api/platform/organization-b2b-public-profile-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import { formatRelativeOrDate } from "@/features/devices/device-presentation";
import { BusinessCreditPolicySection } from "@/features/customers/BusinessCreditPolicySection";
import { BusinessRelationshipContactEditDrawer } from "@/features/customers/BusinessRelationshipContactEditDrawer";
import { CustomerBranchVisibilitySection } from "@/features/customers/CustomerBranchVisibilitySection";
import { RecordPaymentModal } from "@/features/customers/RecordPaymentModal";
import {
  relationshipStatusLabelKey,
  relationshipStatusTone,
  type CustomerListRelationshipStatus,
} from "@/features/customers/CustomerListCard";
import { usePreferences } from "@/hooks/usePreferences";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useWorkspace } from "@/workspace/WorkspaceProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";
import { Clock, PackageOpen, Pencil, Users } from "lucide-react";
import { Link, useNavigate, useParams, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";

function mapRelationshipStatus(raw: string): CustomerListRelationshipStatus {
  switch (raw.trim().toLowerCase()) {
    case "pending":
      return "Pending";
    case "active":
      return "Connected";
    case "declined":
      return "Declined";
    default:
      return "Inactive";
  }
}

function catalogModeLabel(mode: string, allEligible: string, selectedOnly: string): string {
  return mode === "AllEligible" ? allEligible : selectedOnly;
}

function formatRequestSent(iso: string, locale: string): string {
  try {
    return new Date(iso).toLocaleString(locale, {
      dateStyle: "medium",
      timeStyle: "short",
    });
  } catch {
    return iso;
  }
}

export function BusinessCustomerDetailPage() {
  const { t } = useI18n();
  const { preferences } = usePreferences();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const { connectionId } = useParams<{ connectionId: string }>();
  const { sessionGrant } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const online = useBrowserOnline();
  const allowManage = canManageSuppliers(sessionGrant);
  const allowManageCredit = canManageCustomerCreditPolicy(sessionGrant);
  const allowApproveCredit = canApproveCustomerCreditPolicy(sessionGrant);
  const allowManageBranchAccess = canManageCustomerBranchAccess(sessionGrant);
  const allowRepay = canRecordRepayment(sessionGrant);
  const allowStatement = canViewStatement(sessionGrant);
  const [relationshipEditOpen, setRelationshipEditOpen] = useState(false);
  const [recordPaymentOpen, setRecordPaymentOpen] = useState(false);

  useEffect(() => {
    if (searchParams.get("recordPayment") !== "1") {
      return;
    }
    setRecordPaymentOpen(true);
    const next = new URLSearchParams(searchParams);
    next.delete("recordPayment");
    setSearchParams(next, { replace: true });
  }, [searchParams, setSearchParams]);

  const detailQuery = useQuery({
    queryKey: ["business-customers", "detail", workspace?.organizationId, connectionId],
    enabled: Boolean(workspace) && Boolean(connectionId),
    queryFn: ({ signal }) => getBusinessCustomer(workspace!, connectionId!, signal),
  });

  const buyerOrgId = detailQuery.data?.buyerOrganizationId;
  const isDetailConnected =
    (detailQuery.data?.relationshipStatus ?? "").trim().toLowerCase() === "active";

  const utangSummaryQuery = useQuery({
    queryKey: ["business-customers", "utang-summary", workspace?.organizationId, connectionId],
    enabled: Boolean(workspace) && Boolean(connectionId) && online,
    queryFn: ({ signal }) => getBusinessCustomerUtangSummary(workspace!, connectionId!, signal),
  });

  const commerceReadinessQuery = useQuery({
    queryKey: ["business-customers", "commerce-readiness", workspace?.organizationId, connectionId],
    enabled: Boolean(workspace) && Boolean(connectionId) && online && isDetailConnected,
    queryFn: ({ signal }) =>
      getSupplierConnectedSupplierCommerceReadiness(workspace!, connectionId!, signal),
  });

  const publicOrgQuery = useQuery({
    queryKey: [
      "business-customers",
      "b2b-public-profile",
      workspace?.organizationId,
      buyerOrgId,
    ],
    enabled: Boolean(workspace?.organizationId) && Boolean(buyerOrgId) && isDetailConnected,
    queryFn: ({ signal }) =>
      getOrganizationB2bPublicProfile(buyerOrgId!, workspace!.organizationId, signal),
  });

  const cancelMutation = useMutation({
    mutationFn: () => cancelConnectionRequest(workspace!, connectionId!),
    onSuccess: async () => {
      showToast(t("customers.business.requestRevoked"), "success");
      await queryClient.invalidateQueries({ queryKey: ["business-customers"] });
      navigate("/customers?kind=businesses");
    },
    onError: (error) => {
      showToast(
        error instanceof PosApiError
          ? (error.problem.detail ?? error.message)
          : t("customers.business.requestRevokeFailed"),
        "error",
      );
    },
  });

  if (!workspace) {
    return <LoadingState label={t("session.loading")} />;
  }

  if (detailQuery.isLoading) {
    return <LoadingState label={t("loading.label")} />;
  }

  if (detailQuery.isError) {
    return (
      <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="business-customer-detail-error">
        <PageHeader
          title={t("customers.business.detailTitle")}
          backTo={pageBackNav.customers.to}
          backLabel={t(pageBackNav.customers.labelKey)}
          backTestId="page-header-back-customers"
        />
        <ErrorState
          title={t("customers.business.loadFailed")}
          detail={
            detailQuery.error instanceof PosApiError
              ? (detailQuery.error.problem.detail ?? detailQuery.error.message)
              : t("customers.business.loadFailedHelp")
          }
          error={detailQuery.error}
          operation="getBusinessCustomer"
        />
        <button
          type="button"
          className="exits-btn exits-btn--secondary self-start"
          data-testid="business-customer-detail-retry"
          onClick={() => void detailQuery.refetch()}
        >
          {t("customers.business.retry")}
        </button>
      </div>
    );
  }

  if (!detailQuery.data) {
    return (
      <EmptyState
        align="center"
        icon={<Users className="size-5" strokeWidth={1.75} />}
        title={t("customers.business.notFound")}
        detail={t("customers.business.notFoundHelp")}
      />
    );
  }

  const customer = detailQuery.data;
  const name =
    customer.organizationDisplayName.trim() || t("customers.business.unknown");
  const relationship = mapRelationshipStatus(customer.relationshipStatus);
  const isConnected = relationship === "Connected";
  const isPending = relationship === "Pending";
  const sellerInitiated = (customer.initiatedByParty ?? "Buyer").toLowerCase() === "supplier";
  const canRevokePending = allowManage && isPending && sellerInitiated && !customer.actionRequired;
  const since =
    isConnected && customer.connectedSinceUtc
      ? formatRelativeOrDate(customer.connectedSinceUtc, new Date(), preferences.locale)
      : null;
  const discountLabel =
    customer.customerDiscountPercent != null && customer.customerDiscountPercent > 0
      ? t("customers.business.discountOff").replace(
          "{percent}",
          String(customer.customerDiscountPercent),
        )
      : t("customers.business.noDiscount");
  const hasRelationshipContact = Boolean(
    customer.organizationMemberId ||
      customer.contactPersonName?.trim() ||
      customer.contactDepartment?.trim() ||
      customer.contactRole?.trim() ||
      customer.contactPhone?.trim() ||
      customer.contactEmail?.trim() ||
      customer.preferredContactMethod?.trim() ||
      customer.deliveryInstructions?.trim() ||
      customer.billingContactNotes?.trim() ||
      customer.internalNotes?.trim(),
  );
  const contactUnavailable =
    customer.contactSource === "OrganizationMember" && customer.organizationMemberAvailable === false;

  return (
    <div
      className="business-customer-detail-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="business-customer-detail"
    >
      <PageHeader
        title={t("customers.business.detailTitle")}
        description={name}
        backTo="/customers?kind=businesses"
        backLabel={t("customers.business.back")}
        backTestId="page-header-back-customers"
      />

      <Card className="customer-ownership-section p-4" data-testid="business-org-information">
        <div className="flex flex-wrap items-center justify-between gap-2">
          <h2 className="customer-ownership-section__title">
            {t("customers.business.orgInformation.title")}
          </h2>
          <span className="customer-ownership-section__hint">
            {t("customers.business.orgInformation.readOnly")}
          </span>
        </div>
        <p className="customer-ownership-section__hint" data-testid="business-org-managed-by">
          {t("customers.business.orgInformation.managedBy").replace("{name}", name)}
        </p>

        <div
          className="mt-2 flex flex-wrap items-center gap-1.5"
          data-testid="business-customer-status-chips"
        >
          <StatusChip tone="success">{t("customers.badge.b2b")}</StatusChip>
          <StatusChip tone={relationshipStatusTone(relationship)}>
            {t(relationshipStatusLabelKey(relationship))}
          </StatusChip>
        </div>

        {since ? (
          <p
            className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted"
            data-testid="business-customer-connected-since"
          >
            {t("customers.business.connectedSince").replace("{when}", since)}
          </p>
        ) : null}

        {relationship === "Declined" ? (
          <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.business.connectionDeclined")}
          </p>
        ) : null}
        {relationship === "Inactive" ? (
          <p className="m-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.business.connectionInactive")}
          </p>
        ) : null}

        {(() => {
          const publicOrg = isConnected ? publicOrgQuery.data : null;
          const displayName = publicOrg?.displayName?.trim() || name;
          const publicId =
            publicOrg?.publicOrganizationId?.trim() || customer.organizationPublicId?.trim() || null;
          const phone = publicOrg?.businessPhone?.trim() || null;
          const email = publicOrg?.businessEmail?.trim() || null;
          const address = publicOrg ? formatPublicBusinessAddress(publicOrg) : null;
          const logoUrl = publicOrg?.logoUrl?.trim() || null;
          return (
            <div className="mt-3 flex flex-col gap-3">
              {logoUrl ? (
                <img
                  src={logoUrl}
                  alt=""
                  className="size-14 rounded-lg object-cover"
                  data-testid="business-org-logo"
                />
              ) : null}
              <dl className="customer-ownership-dl">
                <div>
                  <dt>{t("customers.business.orgInformation.businessName")}</dt>
                  <dd data-testid="business-org-name">{displayName}</dd>
                </div>
                {publicId ? (
                  <div>
                    <dt>{t("customers.business.orgInformation.organizationId")}</dt>
                    <dd data-testid="business-org-exits-id">{publicId}</dd>
                  </div>
                ) : null}
                {phone ? (
                  <div>
                    <dt>{t("customers.business.orgInformation.officialPhone")}</dt>
                    <dd data-testid="business-org-phone">{phone}</dd>
                  </div>
                ) : null}
                {email ? (
                  <div>
                    <dt>{t("customers.business.orgInformation.officialEmail")}</dt>
                    <dd data-testid="business-org-email">{email}</dd>
                  </div>
                ) : null}
                {address ? (
                  <div>
                    <dt>{t("customers.business.orgInformation.businessAddress")}</dt>
                    <dd data-testid="business-org-address">{address}</dd>
                  </div>
                ) : null}
              </dl>
            </div>
          );
        })()}
      </Card>

      {(isConnected || isPending) ? (
        <Card className="customer-ownership-section p-4" data-testid="business-relationship-contact">
          <div className="flex flex-wrap items-center justify-between gap-2">
            <h2 className="customer-ownership-section__title">
              {t("customers.business.relationshipContact.title")}
            </h2>
            {allowManage && hasRelationshipContact ? (
              <Button
                type="button"
                variant="outline"
                data-testid="business-edit-relationship-contact"
                onClick={() => setRelationshipEditOpen(true)}
              >
                <Pencil className="size-4 shrink-0" aria-hidden />
                {t("customers.business.relationshipContact.edit")}
              </Button>
            ) : null}
          </div>
          {hasRelationshipContact ? (
            <>
              {contactUnavailable ? (
                <p
                  className="customer-ownership-section__hint"
                  data-testid="business-contact-unavailable"
                >
                  {t("customers.business.relationshipContact.contactUnavailable")}
                </p>
              ) : null}
              <dl className="customer-ownership-dl">
              <div>
                <dt>{t("customers.business.relationshipContact.contactPerson")}</dt>
                <dd data-testid="business-contact-person">
                  {customer.contactPersonName?.trim() || "—"}
                </dd>
              </div>
              {customer.contactDepartment?.trim() ? (
                <div>
                  <dt>{t("customers.business.relationshipContact.department")}</dt>
                  <dd data-testid="business-contact-department">{customer.contactDepartment}</dd>
                </div>
              ) : null}
              {customer.contactRole?.trim() ? (
                <div>
                  <dt>{t("customers.business.relationshipContact.role")}</dt>
                  <dd>{customer.contactRole}</dd>
                </div>
              ) : null}
              {customer.contactPhone?.trim() ? (
                <div>
                  <dt>{t("customers.business.relationshipContact.phone")}</dt>
                  <dd>{customer.contactPhone}</dd>
                </div>
              ) : null}
              {customer.contactEmail?.trim() ? (
                <div>
                  <dt>{t("customers.business.relationshipContact.email")}</dt>
                  <dd>{customer.contactEmail}</dd>
                </div>
              ) : null}
              {customer.preferredContactMethod?.trim() ? (
                <div>
                  <dt>{t("customers.business.relationshipContact.preferredMethod")}</dt>
                  <dd>{customer.preferredContactMethod}</dd>
                </div>
              ) : null}
              {customer.deliveryInstructions?.trim() ? (
                <div>
                  <dt>{t("customers.business.relationshipContact.deliveryInstructions")}</dt>
                  <dd>{customer.deliveryInstructions}</dd>
                </div>
              ) : null}
              {customer.billingContactNotes?.trim() ? (
                <div>
                  <dt>{t("customers.business.relationshipContact.billingNotes")}</dt>
                  <dd>{customer.billingContactNotes}</dd>
                </div>
              ) : null}
              {customer.internalNotes?.trim() ? (
                <div>
                  <dt>{t("customers.business.relationshipContact.internalNotes")}</dt>
                  <dd data-testid="business-contact-internal-notes">{customer.internalNotes}</dd>
                </div>
              ) : null}
            </dl>
            </>
          ) : (
            <div className="flex flex-col gap-3" data-testid="business-relationship-contact-empty">
              <EmptyState
                align="center"
                icon={<Users className="size-5" strokeWidth={1.75} />}
                title={t("customers.business.relationshipContact.emptyTitle")}
                detail={t("customers.business.relationshipContact.emptyDetail")}
              />
              {allowManage ? (
                <Button
                  type="button"
                  className="self-center"
                  data-testid="business-add-relationship-contact"
                  onClick={() => setRelationshipEditOpen(true)}
                >
                  <Pencil className="size-4 shrink-0" aria-hidden />
                  {t("customers.business.relationshipContact.add")}
                </Button>
              ) : null}
            </div>
          )}
        </Card>
      ) : null}

      {isPending ? (
        <Card
          data-testid="business-customer-pending-banner"
          className="border-[color-mix(in_srgb,var(--exits-warning)_35%,var(--exits-border))]"
        >
          <div className="flex flex-wrap items-start gap-3">
            <span
              className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[color-mix(in_srgb,var(--exits-warning)_14%,transparent)] text-[var(--exits-warning)]"
              aria-hidden
            >
              <Clock className="size-5" />
            </span>
            <div className="min-w-0 flex-1">
              <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("customers.business.connectionRequestPending")}
              </p>
              <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {customer.actionRequired
                  ? t("customers.business.actionRequired").replace("{name}", name)
                  : t("customers.business.waitingForAcceptDetail").replace("{name}", name)}
              </p>
              <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
                <span className="font-medium text-foreground">
                  {t("customers.business.requestSentLabel")}
                </span>
                <span className="mx-1.5 text-muted">·</span>
                <span data-testid="business-customer-request-sent-at">
                  {formatRequestSent(customer.createdAtUtc, preferences.locale)}
                </span>
              </p>
            </div>
          </div>

          <div className="mt-3 flex flex-wrap gap-2">
            {customer.actionRequired ? (
              <Button type="button" asChild data-testid="business-customer-review-request">
                <Link to="/suppliers/connected/requests">{t("customers.business.reviewRequest")}</Link>
              </Button>
            ) : null}
            {canRevokePending ? (
              <Button
                type="button"
                variant="outline"
                data-testid="business-customer-revoke-request"
                disabled={!online || cancelMutation.isPending}
                onClick={() => cancelMutation.mutate()}
              >
                {t("customers.business.revokeRequest")}
              </Button>
            ) : null}
          </div>
        </Card>
      ) : null}

      {isConnected && commerceReadinessQuery.data ? (
        <Card
          className="flex flex-col gap-3 p-3"
          data-testid="business-customer-commerce-readiness"
        >
          <div className="flex flex-wrap items-start justify-between gap-2">
            <div className="min-w-0 flex-1">
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("customers.business.commerceReadinessTitle")}
              </h2>
              <div className="mt-2">
                <StatusChip
                  tone={commerceReadinessQuery.data.isReady ? "success" : "warning"}
                  data-testid="business-customer-commerce-readiness-status"
                >
                  {commerceReadinessQuery.data.isReady
                    ? t("customers.business.commerceReadinessReady")
                    : t("customers.business.commerceReadinessSetupRequired")}
                </StatusChip>
              </div>
              {!commerceReadinessQuery.data.isReady ? (
                <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
                  {t("customers.business.commerceReadinessNotReady")}
                </p>
              ) : null}
            </div>
            {!commerceReadinessQuery.data.isReady && allowManage ? (
              <Button
                type="button"
                asChild
                data-testid="business-customer-complete-setup"
              >
                <Link
                  to={
                    customer.supplierBranchId
                      ? `/org/branches/${customer.supplierBranchId}`
                      : `/suppliers/connected/buyers/${customer.connectionId}/shared-products`
                  }
                >
                  {t("customers.business.commerceReadinessCompleteSetup")}
                </Link>
              </Button>
            ) : null}
          </div>
          <ul
            className="m-0 flex list-none flex-col gap-2 p-0"
            data-testid="business-customer-commerce-readiness-checklist"
          >
            {(commerceReadinessQuery.data.requirements ?? [])
              .filter((item) => item.status !== "NotApplicable")
              .map((item) => (
              <li
                key={item.code}
                className="rounded-md border border-border px-3 py-2"
                data-testid={`commerce-readiness-${item.code}`}
                data-status={item.status}
              >
                <div className="flex flex-wrap items-center justify-between gap-2">
                  <span className="text-[length:var(--exits-text-sm)] font-medium">
                    {item.title}
                  </span>
                  <StatusChip
                    tone={
                      item.status === "Complete"
                        ? "success"
                        : item.status === "Missing"
                          ? "warning"
                          : "neutral"
                    }
                  >
                    {t(`customers.business.commerceReadinessStatus.${item.status}`)}
                  </StatusChip>
                </div>
                {item.status === "Missing" && item.detail ? (
                  <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {item.detail}
                  </p>
                ) : null}
              </li>
            ))}
          </ul>
        </Card>
      ) : null}

      {workspace && connectionId ? (
        <div
          className={
            isConnected
              ? "grid gap-3 lg:grid-cols-2 lg:items-start"
              : undefined
          }
          data-testid="business-customer-branch-catalog-row"
        >
          <CustomerBranchVisibilitySection
            workspace={workspace}
            organizationId={workspace.organizationId}
            customerId={connectionId}
            online={online}
            canManage={allowManageBranchAccess}
            kind="business"
            helpKey="customers.business.branchAccessHelp"
          />

          {isConnected ? (
            <Card className="flex flex-col gap-3 p-3" data-testid="business-customer-catalog-summary">
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("customers.business.catalogPricing")}
              </h2>
              <dl className="m-0 grid gap-2 text-[length:var(--exits-text-sm)]">
                <div className="flex justify-between gap-3">
                  <dt className="text-muted">{t("customers.business.catalogMode")}</dt>
                  <dd className="m-0 font-medium">
                    {catalogModeLabel(
                      customer.catalogSharingMode,
                      t("customers.business.modeAllEligible"),
                      t("customers.business.modeSelectedOnly"),
                    )}
                  </dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted">{t("customers.business.shared")}</dt>
                  <dd className="m-0 font-medium">{customer.sharedCount}</dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted">{t("customers.business.excluded")}</dt>
                  <dd className="m-0 font-medium">{customer.excludedCount}</dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted">{t("customers.business.overrides")}</dt>
                  <dd className="m-0 font-medium">{customer.overrideCount}</dd>
                </div>
                <div className="flex justify-between gap-3">
                  <dt className="text-muted">{t("customers.business.customerPricing")}</dt>
                  <dd className="m-0 font-medium">{discountLabel}</dd>
                </div>
              </dl>
              {allowManage ? (
                <div className="mt-auto flex flex-wrap gap-2">
                  <Button type="button" asChild data-testid="business-customer-manage-catalog">
                    <Link to={`/suppliers/connected/buyers/${customer.connectionId}/shared-products`}>
                      <PackageOpen className="size-4 shrink-0" aria-hidden />
                      {t("customers.business.manageCatalog")}
                    </Link>
                  </Button>
                </div>
              ) : null}
            </Card>
          ) : null}
        </div>
      ) : null}

      {workspace && connectionId && (isConnected || isPending) ? (
        <BusinessCreditPolicySection
          workspace={workspace}
          connectionId={connectionId}
          online={online}
          canManage={allowManageCredit && isConnected}
          canApprove={allowApproveCredit && isConnected}
          canRecordPayment={allowRepay && isConnected}
          canViewStatement={allowStatement && isConnected}
          subjectIdentity={[name, customer.organizationPublicId?.trim()]
            .filter((part): part is string => Boolean(part))
            .join(" · ")}
          onRecordPayment={isConnected ? () => setRecordPaymentOpen(true) : undefined}
        />
      ) : null}

      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
        {t("customers.business.identityNote")}
      </p>

      <p className="m-0">
        <Link
          className="text-[length:var(--exits-text-sm)]"
          to="/suppliers/connected/requests"
          data-testid="business-customer-incoming-link"
        >
          {t("customers.business.incomingRequests")}
        </Link>
      </p>

      {(isConnected || isPending) && workspace ? (
        <BusinessRelationshipContactEditDrawer
          open={relationshipEditOpen}
          onClose={() => setRelationshipEditOpen(false)}
          workspace={workspace}
          customer={customer}
        />
      ) : null}

      {workspace && connectionId && isConnected ? (
        <RecordPaymentModal
          open={recordPaymentOpen}
          onOpenChange={setRecordPaymentOpen}
          customerKind="business"
          connectionId={connectionId}
          displayName={name}
          outstandingBalance={utangSummaryQuery.data?.outstandingAmount ?? 0}
          onSuccess={() => {
            void queryClient.invalidateQueries({
              queryKey: ["business-customers", "utang-summary", workspace.organizationId, connectionId],
            });
          }}
        />
      ) : null}
    </div>
  );
}
