import { useEffect, useMemo, useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  ClipboardList,
  FileText,
  Network,
  Package,
  Percent,
  Truck,
  WalletCards,
} from "lucide-react";
import { canManageSuppliers, hasOrganizationManagementAuthority } from "@/access/pos-capabilities";
import {
  getConnectedCommerceOverview,
  getOrganizationConnectedCommerceSettings,
  getOrganizationOfferDelivery,
  updateOrganizationConnectedCommerceSettings,
  updateOrganizationOfferDelivery,
  type OrganizationConnectedCommerceSettingsDto,
} from "@/api/pos/pos-connected-commerce-client";
import { listCatalogCategories } from "@/api/pos/pos-catalog-client";
import { listPaymentMethods } from "@/api/pos/pos-payment-methods-client";
import {
  getOrganizationSalesDocumentCapability,
} from "@/api/platform/organization-sales-document-capability-client";
import { getOrganizationOnlineSupplierPaymentsCapability } from "@/api/platform/organization-online-supplier-payments-client";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Switch } from "@/components/ui/switch";
import { ErrorState } from "@/components/exits/ErrorState";
import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { ProductCategoryMultiSelect } from "@/components/exits/ProductCategoryMultiSelect";
import { StatusChip } from "@/components/exits/StatusChip";
import { UnderlineTabBar } from "@/components/exits/UnderlineTabBar";
import { useToast } from "@/components/exits/ToastProvider";
import { invalidateOrganizationOfferDeliveryQueries } from "@/features/branches/offer-delivery-queries";
import {
  parseConnectedCommerceTab,
  type ConnectedCommerceTab,
} from "@/features/connected-commerce/connected-commerce-tabs";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { pageBackNav } from "@/navigation/page-back-nav";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

const TIMING_OPTIONS = [
  { code: "PayBeforeFulfillment", labelKey: "connectedCommerce.timing.payBefore" as const },
  { code: "PayOnDeliveryOrReceipt", labelKey: "connectedCommerce.timing.payOnDelivery" as const },
  { code: "SupplierCredit", labelKey: "connectedCommerce.timing.supplierCredit" as const },
] as const;

type TimingCode = (typeof TIMING_OPTIONS)[number]["code"];

function statusTone(status: string): "success" | "warning" | "danger" | "neutral" | "info" {
  const s = status.toLowerCase();
  if (s.includes("ready") || s.includes("ok") || s.includes("available") || s.includes("approved")) {
    return "success";
  }
  if (s.includes("required") || s.includes("incomplete") || s.includes("setup")) {
    return "warning";
  }
  if (s.includes("unavailable") || s.includes("disabled") || s.includes("suspended") || s.includes("reject")) {
    return "danger";
  }
  return "info";
}

export function ConnectedCommerceSettingsPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const { sessionGrant } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const [searchParams, setSearchParams] = useSearchParams();
  const tab = parseConnectedCommerceTab(searchParams.get("tab"));
  const smartBack = usePageSmartBack({
    fallback: pageBackNav.org.to,
    backLabel: t(pageBackNav.org.labelKey),
  });

  // Org settings: organizationId is enough (Manage Business is often branch-null).
  const organizationId = workspace?.organizationId ?? null;

  const canEdit =
    hasOrganizationManagementAuthority(sessionGrant) || canManageSuppliers(sessionGrant);

  const overviewQuery = useQuery({
    queryKey: ["connected-commerce", "overview", organizationId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => getConnectedCommerceOverview(workspace!, signal),
  });

  const settingsQuery = useQuery({
    queryKey: ["connected-commerce", "settings", organizationId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => getOrganizationConnectedCommerceSettings(workspace!, signal),
  });

  const offerQuery = useQuery({
    queryKey: ["connected-commerce", "offer-delivery", organizationId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => getOrganizationOfferDelivery(workspace!, signal),
  });

  const paymentMethodsQuery = useQuery({
    queryKey: ["payment-methods", organizationId],
    enabled: Boolean(workspace) && (tab === "payments" || tab === "overview"),
    queryFn: ({ signal }) => listPaymentMethods(workspace!, signal),
  });

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", organizationId, "connected-commerce"],
    enabled: Boolean(workspace) && tab === "catalog",
    queryFn: ({ signal }) =>
      listCatalogCategories(workspace!, { status: "Active", pageSize: 200 }, signal),
  });

  const onlinePaymentsQuery = useQuery({
    queryKey: ["online-supplier-payments", organizationId],
    enabled: Boolean(organizationId) && (tab === "payments" || tab === "overview" || tab === "documents"),
    queryFn: ({ signal }) =>
      getOrganizationOnlineSupplierPaymentsCapability(organizationId!, signal),
  });

  const birQuery = useQuery({
    queryKey: ["sales-document-capability", organizationId],
    enabled: Boolean(organizationId) && (tab === "documents" || tab === "overview"),
    queryFn: ({ signal }) => getOrganizationSalesDocumentCapability(organizationId!, signal),
  });

  const [draft, setDraft] = useState<OrganizationConnectedCommerceSettingsDto | null>(null);
  const [addCategoryIds, setAddCategoryIds] = useState<string[]>([]);

  const categoryNameById = useMemo(() => {
    const map = new Map<string, string>();
    for (const c of categoriesQuery.data?.items ?? []) {
      map.set(c.categoryId, c.name);
    }
    return map;
  }, [categoriesQuery.data]);

  const availableCatalogCategories = useMemo(
    () =>
      (categoriesQuery.data?.items ?? []).filter(
        (c) => !(draft?.categoryRules ?? []).some((r) => r.categoryId === c.categoryId),
      ),
    [categoriesQuery.data, draft?.categoryRules],
  );

  useEffect(() => {
    const available = new Set(availableCatalogCategories.map((c) => c.categoryId));
    setAddCategoryIds((current) => current.filter((id) => available.has(id)));
  }, [availableCatalogCategories]);

  useEffect(() => {
    if (settingsQuery.data) {
      setDraft(settingsQuery.data);
    }
  }, [settingsQuery.data]);

  const enabledDefaults = useMemo(() => {
    if (!draft) {
      return [] as typeof TIMING_OPTIONS[number][];
    }
    return TIMING_OPTIONS.filter((option) => {
      if (option.code === "PayBeforeFulfillment") {
        return draft.allowPayBeforeFulfillment;
      }
      if (option.code === "PayOnDeliveryOrReceipt") {
        return draft.allowPayOnDeliveryOrReceipt;
      }
      return draft.allowSupplierCredit;
    });
  }, [draft]);

  useEffect(() => {
    if (!draft || enabledDefaults.length === 0) {
      return;
    }
    const enabledCodes = enabledDefaults.map((o) => o.code);
    if (!enabledCodes.includes(draft.defaultPaymentTiming as TimingCode)) {
      setDraft((current) =>
        current ? { ...current, defaultPaymentTiming: enabledCodes[0]! } : current,
      );
    }
  }, [draft, enabledDefaults]);

  const saveMutation = useMutation({
    mutationFn: async () => {
      if (!workspace || !draft) {
        throw new Error("missing draft");
      }
      return updateOrganizationConnectedCommerceSettings(workspace, {
        allowPayBeforeFulfillment: draft.allowPayBeforeFulfillment,
        allowPayOnDeliveryOrReceipt: draft.allowPayOnDeliveryOrReceipt,
        allowSupplierCredit: draft.allowSupplierCredit,
        defaultPaymentTiming: draft.defaultPaymentTiming,
        defaultB2bDiscountPercent: draft.defaultB2bDiscountPercent,
        proposalReservationHoldHours: draft.proposalReservationHoldHours,
        categoryRules: draft.categoryRules.map((r) => ({
          categoryId: r.categoryId,
          discountPercent: r.discountPercent,
        })),
      });
    },
    onSuccess: (saved) => {
      setDraft(saved);
      void queryClient.invalidateQueries({ queryKey: ["connected-commerce"] });
      showToast(t("connectedCommerce.saved"), "success");
    },
    onError: () => showToast(t("connectedCommerce.saveFailed"), "error"),
  });

  const offerMutation = useMutation({
    mutationFn: async (offerDelivery: boolean) => {
      if (!workspace) throw new Error("missing workspace");
      return updateOrganizationOfferDelivery(workspace, offerDelivery);
    },
    onSuccess: async () => {
      await invalidateOrganizationOfferDeliveryQueries(queryClient, organizationId);
      showToast(t("connectedCommerce.saved"), "success");
    },
    onError: () => showToast(t("connectedCommerce.saveFailed"), "error"),
  });

  function setTab(next: ConnectedCommerceTab) {
    const params = new URLSearchParams(searchParams);
    if (next === "overview") {
      params.delete("tab");
    } else {
      params.set("tab", next);
    }
    setSearchParams(params, { replace: true });
  }

  if (!workspace || !organizationId) {
    return (
      <ErrorState
        title={t("connectedCommerce.orgRequired")}
        detail={t("connectedCommerce.orgRequiredDetail")}
      />
    );
  }

  const loading = overviewQuery.isLoading || settingsQuery.isLoading;

  return (
    <div className="flex flex-col gap-4" data-testid="connected-commerce-settings-page">
      <PageHeader
        title={t("connectedCommerce.title")}
        description={t("connectedCommerce.lede")}
        {...smartBack}
      />

      <UnderlineTabBar
        ariaLabel={t("connectedCommerce.tabsLabel")}
        testId="connected-commerce-tabs"
        activeKey={tab}
        onChange={(key) => setTab(parseConnectedCommerceTab(key))}
        items={[
          { key: "overview", label: t("connectedCommerce.tab.overview"), icon: Network },
          { key: "fulfillment", label: t("connectedCommerce.tab.fulfillment"), icon: Truck },
          { key: "payments", label: t("connectedCommerce.tab.payments"), icon: WalletCards },
          { key: "catalog", label: t("connectedCommerce.tab.catalog"), icon: Percent },
          { key: "orders", label: t("connectedCommerce.tab.orders"), icon: ClipboardList },
          { key: "documents", label: t("connectedCommerce.tab.documents"), icon: FileText },
        ]}
      />

      {loading ? <LoadingState label={t("loading.label")} /> : null}

      {!loading && tab === "overview" ? (
        <div className="grid gap-3 md:grid-cols-2" data-testid="connected-commerce-overview">
          {[
            {
              title: t("connectedCommerce.summary.fulfillment"),
              status:
                offerQuery.data?.offerDelivery === true
                  ? t("connectedCommerce.offerDeliveryOn")
                  : t("connectedCommerce.offerDeliveryOff"),
              icon: Truck,
            },
            {
              title: t("connectedCommerce.summary.payments"),
              status: overviewQuery.data?.settings.defaultPaymentTiming
                ?? t("connectedCommerce.platformUnknown"),
              icon: WalletCards,
            },
            {
              title: t("connectedCommerce.summary.catalog"),
              status: t("connectedCommerce.optionalReady").replace(
                "{n}",
                String(overviewQuery.data?.settings.defaultB2bDiscountPercent ?? 0),
              ),
              icon: Percent,
            },
            {
              title: t("connectedCommerce.summary.customers"),
              status: String(
                (overviewQuery.data?.activeBusinessCustomerCount ?? 0)
                + (overviewQuery.data?.pendingBusinessCustomerCount ?? 0),
              ),
              icon: Package,
            },
            {
              title: t("connectedCommerce.summary.onlinePayments"),
              status: onlinePaymentsQuery.data?.status ?? t("connectedCommerce.platformUnknown"),
              icon: WalletCards,
            },
            {
              title: t("connectedCommerce.summary.documents"),
              status:
                birQuery.data?.complianceEligibilityStatus
                ?? t("connectedCommerce.platformUnknown"),
              icon: FileText,
            },
          ].map((card) => (
            <Card key={card.title} className="flex flex-col gap-2 p-3" treatment="bordered">
              <div className="flex items-center gap-2">
                <card.icon className="size-4 text-muted" aria-hidden />
                <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{card.title}</h2>
              </div>
              <StatusChip tone={statusTone(card.status)} appearance="outline">
                {card.status}
              </StatusChip>
            </Card>
          ))}

          <Card className="flex flex-col gap-2 p-3 md:col-span-2" treatment="bordered">
            <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("connectedCommerce.incompleteTitle")}
            </h2>
            <ul className="m-0 flex list-none flex-col gap-2 p-0">
              <li className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-[length:var(--exits-text-sm)]">
                  {t("connectedCommerce.linkBranches")}
                </span>
                <Button asChild appearance="outline" size="default">
                  <Link to="/org/branches">{t("connectedCommerce.open")}</Link>
                </Button>
              </li>
              <li className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-[length:var(--exits-text-sm)]">
                  {t("connectedCommerce.linkPaymentMethods")}
                </span>
                <Button asChild appearance="outline" size="default">
                  <Link to="/org/payment-methods">{t("connectedCommerce.open")}</Link>
                </Button>
              </li>
              <li className="flex flex-wrap items-center justify-between gap-2">
                <span className="text-[length:var(--exits-text-sm)]">
                  {t("connectedCommerce.summary.onlinePayments")}{" "}
                  <span className="text-muted">({t("connectedCommerce.informational")})</span>
                </span>
                <StatusChip tone="neutral" appearance="outline">
                  {onlinePaymentsQuery.data?.status ?? t("connectedCommerce.platformUnknown")}
                </StatusChip>
              </li>
            </ul>
          </Card>
        </div>
      ) : null}

      {!loading && tab === "fulfillment" ? (
        <div className="flex flex-col gap-3" data-testid="connected-commerce-fulfillment">
          <Card className="flex flex-col gap-3 p-3" treatment="bordered">
            <div className="flex items-center justify-between gap-3">
              <div>
                <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("connectedCommerce.offerDelivery")}
                </h2>
                <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                  {t("connectedCommerce.offerDeliveryHelp")}
                </p>
              </div>
              <Switch
                checked={offerQuery.data?.offerDelivery === true}
                disabled={!canEdit || offerMutation.isPending}
                onCheckedChange={(next) => offerMutation.mutate(next)}
                data-testid="connected-commerce-offer-delivery"
              />
            </div>
            <Notice tone="info">{t("connectedCommerce.customerDeliveryNote")}</Notice>
          </Card>

          <Card className="flex flex-col gap-2 p-3" treatment="bordered">
            <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("connectedCommerce.branchReadiness")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("connectedCommerce.branchReadinessHelp")}
            </p>
            <Button asChild appearance="outline" size="default" className="self-start">
              <Link to="/org/branches">{t("connectedCommerce.manageFulfillment")}</Link>
            </Button>
          </Card>
        </div>
      ) : null}

      {!loading && tab === "payments" && draft ? (
        <div className="flex flex-col gap-3" data-testid="connected-commerce-payments">
          <Card className="flex flex-col gap-3 p-3" treatment="bordered">
            <div className="min-w-0">
              <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
                {t("connectedCommerce.paymentTiming")}
              </h2>
              <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {t("connectedCommerce.paymentTimingHelp")}
              </p>
            </div>

            <div className="grid grid-cols-3 gap-2">
              {TIMING_OPTIONS.map((option) => {
                const checked =
                  option.code === "PayBeforeFulfillment"
                    ? draft.allowPayBeforeFulfillment
                    : option.code === "PayOnDeliveryOrReceipt"
                      ? draft.allowPayOnDeliveryOrReceipt
                      : draft.allowSupplierCredit;
                return (
                  <label
                    key={option.code}
                    className="flex min-w-0 flex-col gap-2 rounded-md border border-border px-3 py-2"
                  >
                    <span className="flex items-start gap-2">
                      <input
                        type="checkbox"
                        className="mt-1 shrink-0"
                        checked={checked}
                        disabled={!canEdit}
                        onChange={(e) => {
                          const next = e.target.checked;
                          setDraft((current) =>
                            current
                              ? {
                                  ...current,
                                  allowPayBeforeFulfillment:
                                    option.code === "PayBeforeFulfillment"
                                      ? next
                                      : current.allowPayBeforeFulfillment,
                                  allowPayOnDeliveryOrReceipt:
                                    option.code === "PayOnDeliveryOrReceipt"
                                      ? next
                                      : current.allowPayOnDeliveryOrReceipt,
                                  allowSupplierCredit:
                                    option.code === "SupplierCredit"
                                      ? next
                                      : current.allowSupplierCredit,
                                }
                              : current,
                          );
                        }}
                        data-testid={`connected-commerce-timing-${option.code}`}
                      />
                      <span className="min-w-0 flex-1 text-[length:var(--exits-text-sm)] font-medium">
                        {t(option.labelKey)}
                      </span>
                    </span>
                    {checked ? (
                      <StatusChip tone="success" appearance="outline">
                        {t("customers.business.connectedCommerce.effective")}
                      </StatusChip>
                    ) : (
                      <StatusChip tone="neutral" appearance="outline">
                        {t("customers.business.connectedCommerce.notEffective")}
                      </StatusChip>
                    )}
                  </label>
                );
              })}
            </div>

            <div className="flex flex-col gap-1.5">
              <span className="text-[length:var(--exits-text-sm)] font-normal">
                {t("connectedCommerce.defaultTiming")}
              </span>
              <ExitsPillSelect
                aria-label={t("connectedCommerce.defaultTiming")}
                value={
                  enabledDefaults.some(
                    (option) => option.code === draft.defaultPaymentTiming,
                  )
                    ? (draft.defaultPaymentTiming as TimingCode)
                    : (enabledDefaults[0]?.code ?? (draft.defaultPaymentTiming as TimingCode))
                }
                onChange={(next) =>
                  setDraft((current) =>
                    current ? { ...current, defaultPaymentTiming: next } : current,
                  )
                }
                disabled={!canEdit || enabledDefaults.length === 0}
                options={enabledDefaults.map((option) => ({
                  value: option.code,
                  label: t(option.labelKey),
                }))}
                className={cn(
                  "w-full gap-2 [&>button]:min-w-0 [&>button]:w-full [&>button>span]:w-full",
                  enabledDefaults.length >= 3
                    ? "grid grid-cols-3"
                    : enabledDefaults.length === 2
                      ? "grid grid-cols-2"
                      : "flex",
                )}
                testId="connected-commerce-default-timing"
              />
            </div>

            {canEdit ? (
              <Button
                type="button"
                className="w-full"
                disabled={saveMutation.isPending}
                onClick={() => saveMutation.mutate()}
                data-testid="connected-commerce-payments-save"
              >
                {t("connectedCommerce.save")}
              </Button>
            ) : null}
          </Card>

          <Card className="flex flex-col gap-2 p-3" treatment="bordered">
            <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("connectedCommerce.acceptedMethods")}
            </h2>
            <ul className="m-0 flex list-none flex-col gap-1 p-0 text-[length:var(--exits-text-sm)]">
              {(paymentMethodsQuery.data ?? [])
                .filter((m) => m.isEnabled || m.availability === "BuiltIn")
                .map((m) => (
                  <li key={m.methodCode}>{m.displayName ?? m.methodCode}</li>
                ))}
            </ul>
            <Button asChild appearance="outline" size="default" className="self-start">
              <Link to="/org/payment-methods">{t("connectedCommerce.managePaymentMethods")}</Link>
            </Button>
          </Card>

          <Card className="flex flex-col gap-2 p-3" treatment="bordered">
            <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("connectedCommerce.onlinePayments")}
            </h2>
            <StatusChip tone="neutral" appearance="outline">
              {onlinePaymentsQuery.data?.status ?? t("connectedCommerce.platformUnknown")}
            </StatusChip>
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {t("connectedCommerce.onlinePaymentsReadonly")}
            </p>
          </Card>
        </div>
      ) : null}

      {!loading && tab === "catalog" && draft ? (
        <div className="flex flex-col gap-3" data-testid="connected-commerce-catalog">
          <Notice tone="info">{t("connectedCommerce.catalogHelp")}</Notice>
          <Card className="flex flex-col gap-3 p-3" treatment="bordered">
            <Input
              label={t("connectedCommerce.defaultDiscount")}
              type="number"
              min={0}
              max={100}
              step="0.01"
              value={draft.defaultB2bDiscountPercent}
              disabled={!canEdit}
              onChange={(e) =>
                setDraft((current) =>
                  current
                    ? {
                        ...current,
                        defaultB2bDiscountPercent: Number(e.target.value || 0),
                      }
                    : current,
                )
              }
              data-testid="connected-commerce-default-discount"
            />
            <div>
              <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("connectedCommerce.categoryRules")}
              </h2>
              <p className="mb-2 mt-1 text-[length:var(--exits-text-xs)] text-muted">
                {t("connectedCommerce.categoryRulesCount").replace(
                  "{n}",
                  String(draft.categoryRules.length),
                )}
              </p>
              {draft.categoryRules.length === 0 ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("connectedCommerce.noCategoryRules")}
                </p>
              ) : (
                <ul className="m-0 flex list-none flex-col gap-2 p-0">
                  {draft.categoryRules.map((rule) => (
                    <li
                      key={rule.categoryId}
                      className="grid grid-cols-[1fr_6rem_auto] items-center gap-2"
                    >
                      <span className="truncate text-[length:var(--exits-text-sm)]">
                        {categoryNameById.get(rule.categoryId) ?? rule.categoryId}
                      </span>
                      <Input
                        label="%"
                        type="number"
                        min={0}
                        max={100}
                        step="0.01"
                        value={rule.discountPercent}
                        disabled={!canEdit}
                        onChange={(e) => {
                          const next = Number(e.target.value || 0);
                          setDraft((current) =>
                            current
                              ? {
                                  ...current,
                                  categoryRules: current.categoryRules.map((r) =>
                                    r.categoryId === rule.categoryId
                                      ? { ...r, discountPercent: next }
                                      : r,
                                  ),
                                }
                              : current,
                          );
                        }}
                      />
                      <Button
                        type="button"
                        appearance="ghost"
                        intent="danger"
                        size="default"
                        disabled={!canEdit}
                        onClick={() =>
                          setDraft((current) =>
                            current
                              ? {
                                  ...current,
                                  categoryRules: current.categoryRules.filter(
                                    (r) => r.categoryId !== rule.categoryId,
                                  ),
                                }
                              : current,
                          )
                        }
                      >
                        {t("connectedCommerce.removeRule")}
                      </Button>
                    </li>
                  ))}
                </ul>
              )}
              {canEdit && availableCatalogCategories.length > 0 ? (
                <div className="mt-2 flex flex-col gap-2">
                  <ProductCategoryMultiSelect
                    label={t("connectedCommerce.addCategoryRule")}
                    categories={availableCatalogCategories.map((c) => ({
                      categoryId: c.categoryId,
                      name: c.name,
                    }))}
                    selectedIds={addCategoryIds}
                    onChange={setAddCategoryIds}
                    placeholder={t("connectedCommerce.selectCategory")}
                    selectedCountLabel={(count) =>
                      t("purchasing.categoriesSelected").replace("{count}", String(count))
                    }
                    selectAllLabel={t("purchasing.selectAllCategories")}
                    clearAllLabel={t("purchasing.deselectAllCategories")}
                    searchPlaceholder={t("catalog.searchCategories")}
                    menuLabel={t("connectedCommerce.addCategoryRule")}
                    testId="connected-commerce-add-category"
                  />
                  <Button
                    type="button"
                    appearance="outline"
                    size="default"
                    className="self-start"
                    disabled={addCategoryIds.length === 0}
                    onClick={() => {
                      if (addCategoryIds.length === 0) {
                        return;
                      }
                      setDraft((current) => {
                        if (!current) {
                          return current;
                        }
                        const existing = new Set(current.categoryRules.map((r) => r.categoryId));
                        const additions = addCategoryIds
                          .filter((id) => !existing.has(id))
                          .map((categoryId) => ({ categoryId, discountPercent: 0 }));
                        return {
                          ...current,
                          categoryRules: [...current.categoryRules, ...additions],
                        };
                      });
                      setAddCategoryIds([]);
                    }}
                    data-testid="connected-commerce-add-rule"
                  >
                    {t("connectedCommerce.addCategoryRule")}
                  </Button>
                </div>
              ) : null}
            </div>
          </Card>
          {canEdit ? (
            <Button
              type="button"
              disabled={saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
              data-testid="connected-commerce-catalog-save"
            >
              {t("connectedCommerce.save")}
            </Button>
          ) : null}
        </div>
      ) : null}

      {!loading && tab === "orders" && draft ? (
        <div className="flex flex-col gap-3" data-testid="connected-commerce-orders">
          <Card className="flex flex-col gap-3 p-3" treatment="bordered">
            <Input
              label={t("connectedCommerce.proposalHoldHours")}
              type="number"
              min={1}
              max={72}
              step={1}
              value={draft.proposalReservationHoldHours}
              disabled={!canEdit}
              onChange={(e) =>
                setDraft((current) =>
                  current
                    ? {
                        ...current,
                        proposalReservationHoldHours: Number(e.target.value || 24),
                      }
                    : current,
                )
              }
              data-testid="connected-commerce-hold-hours"
            />
            <Notice tone="info">{t("connectedCommerce.ordersRules")}</Notice>
          </Card>
          {canEdit ? (
            <Button
              type="button"
              disabled={saveMutation.isPending}
              onClick={() => saveMutation.mutate()}
              data-testid="connected-commerce-orders-save"
            >
              {t("connectedCommerce.save")}
            </Button>
          ) : null}
        </div>
      ) : null}

      {!loading && tab === "documents" ? (
        <div className="flex flex-col gap-3" data-testid="connected-commerce-documents">
          <Card className="flex flex-col gap-2 p-3" treatment="bordered">
            <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("connectedCommerce.birStatus")}
            </h2>
            <StatusChip tone="neutral" appearance="outline">
              {birQuery.data?.complianceEligibilityStatus ?? t("connectedCommerce.platformUnknown")}
            </StatusChip>
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {t("connectedCommerce.birReadonly")}
            </p>
          </Card>
          <Card className="flex flex-col gap-2 p-3" treatment="bordered">
            <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("connectedCommerce.documentPrefs")}
            </h2>
            <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
              {t("connectedCommerce.documentPrefsDevice")}
            </p>
            <Button asChild appearance="outline" size="default" className="self-start">
              <Link to="/org/documents-printing">{t("connectedCommerce.openDocuments")}</Link>
            </Button>
          </Card>
        </div>
      ) : null}
    </div>
  );
}
