import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  getBusinessCustomerPaymentTimingOverride,
  getBusinessCustomerPricingOverrides,
  getOrganizationConnectedCommerceSettings,
  updateBusinessCustomerPaymentTimingOverride,
  updateBusinessCustomerPricingOverrides,
} from "@/api/pos/pos-connected-commerce-client";
import { listCatalogCategories } from "@/api/pos/pos-catalog-client";
import { PosApiError, type PosWorkspaceScope } from "@/api/pos/pos-http";
import { BranchFulfillmentSwitch } from "@/features/branches/BranchFulfillmentSwitch";
import { CategoryPricingOverridesPanel } from "@/features/connected-commerce/CategoryPricingOverridesPanel";
import { BottomSheet } from "@/components/exits/SheetDialog";
import { ExitsPillSelect } from "@/components/exits/ExitsPillSelect";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Notice } from "@/components/exits/Notice";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";

const TIMING_OPTIONS = [
  {
    code: "PayBeforeFulfillment",
    labelKey: "connectedCommerce.timing.payBefore" as const,
    orgFlag: "allowPayBeforeFulfillment" as const,
    customerFlag: "allowPayBeforeFulfillment" as const,
    effectiveFlag: "effectiveAllowPayBeforeFulfillment" as const,
  },
  {
    code: "PayOnDeliveryOrReceipt",
    labelKey: "connectedCommerce.timing.payOnDelivery" as const,
    orgFlag: "allowPayOnDeliveryOrReceipt" as const,
    customerFlag: "allowPayOnDeliveryOrReceipt" as const,
    effectiveFlag: "effectiveAllowPayOnDeliveryOrReceipt" as const,
  },
  {
    code: "SupplierCredit",
    labelKey: "connectedCommerce.timing.supplierCredit" as const,
    orgFlag: "allowSupplierCredit" as const,
    customerFlag: "allowSupplierCredit" as const,
    effectiveFlag: "effectiveAllowSupplierCredit" as const,
  },
] as const;

type TimingCode = (typeof TIMING_OPTIONS)[number]["code"];

type BusinessCustomerConnectedCommerceSectionProps = {
  workspace: PosWorkspaceScope;
  connectionId: string;
  online: boolean;
  canManage: boolean;
};

export function BusinessCustomerConnectedCommerceSection({
  workspace,
  connectionId,
  online,
  canManage,
}: BusinessCustomerConnectedCommerceSectionProps) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();

  const orgSettingsQuery = useQuery({
    queryKey: ["connected-commerce", "settings", workspace.organizationId],
    queryFn: ({ signal }) => getOrganizationConnectedCommerceSettings(workspace, signal),
    enabled: online,
  });

  const timingQuery = useQuery({
    queryKey: ["connected-commerce", "payment-timing", connectionId],
    queryFn: ({ signal }) =>
      getBusinessCustomerPaymentTimingOverride(workspace, connectionId, signal),
    enabled: online,
  });

  const pricingQuery = useQuery({
    queryKey: ["connected-commerce", "pricing", connectionId],
    queryFn: ({ signal }) =>
      getBusinessCustomerPricingOverrides(workspace, connectionId, signal),
    enabled: online,
  });

  const categoriesQuery = useQuery({
    queryKey: ["catalog", "categories", workspace.organizationId, "connected-commerce-bc"],
    queryFn: ({ signal }) =>
      listCatalogCategories(workspace, { status: "Active", pageSize: 200 }, signal),
    enabled: online,
  });

  const [useOrgDefaults, setUseOrgDefaults] = useState(true);
  const [allowBefore, setAllowBefore] = useState(true);
  const [allowOnDelivery, setAllowOnDelivery] = useState(true);
  const [allowCredit, setAllowCredit] = useState(false);
  const [defaultTiming, setDefaultTiming] = useState<TimingCode>("PayBeforeFulfillment");
  const [customerDiscount, setCustomerDiscount] = useState<string>("");
  const [categoryOverrides, setCategoryOverrides] = useState<
    Array<{ categoryId: string; discountPercent: number }>
  >([]);
  const [categoryOverridesOpen, setCategoryOverridesOpen] = useState(false);

  useEffect(() => {
    if (!timingQuery.data) {
      return;
    }
    setUseOrgDefaults(timingQuery.data.useOrganizationPaymentTimingDefaults);
    setAllowBefore(timingQuery.data.allowPayBeforeFulfillment);
    setAllowOnDelivery(timingQuery.data.allowPayOnDeliveryOrReceipt);
    setAllowCredit(timingQuery.data.allowSupplierCredit);
    setDefaultTiming(timingQuery.data.customerDefaultPaymentTiming as TimingCode);
  }, [timingQuery.data]);

  useEffect(() => {
    if (!pricingQuery.data) {
      return;
    }
    setCustomerDiscount(
      pricingQuery.data.customerDiscountPercent == null
        ? ""
        : String(pricingQuery.data.customerDiscountPercent),
    );
    setCategoryOverrides(pricingQuery.data.categoryOverrides.map((r) => ({ ...r })));
  }, [pricingQuery.data]);

  const org = orgSettingsQuery.data;

  const timingMutation = useMutation({
    mutationFn: () =>
      updateBusinessCustomerPaymentTimingOverride(workspace, connectionId, {
        useOrganizationPaymentTimingDefaults: useOrgDefaults,
        allowPayBeforeFulfillment: allowBefore,
        allowPayOnDeliveryOrReceipt: allowOnDelivery,
        allowSupplierCredit: allowCredit,
        customerDefaultPaymentTiming: defaultTiming,
      }),
    onSuccess: (data) => {
      void queryClient.invalidateQueries({
        queryKey: ["connected-commerce", "payment-timing", connectionId],
      });
      setUseOrgDefaults(data.useOrganizationPaymentTimingDefaults);
      setAllowBefore(data.allowPayBeforeFulfillment);
      setAllowOnDelivery(data.allowPayOnDeliveryOrReceipt);
      setAllowCredit(data.allowSupplierCredit);
      setDefaultTiming(data.customerDefaultPaymentTiming as TimingCode);
      showToast({ tone: "success", title: t("customers.business.connectedCommerce.saved") });
    },
    onError: (error) => {
      showToast({
        tone: "error",
        title:
          error instanceof PosApiError && error.problem.detail
            ? error.problem.detail
            : t("customers.business.connectedCommerce.saveFailed"),
      });
    },
  });

  const pricingMutation = useMutation({
    mutationFn: () => {
      const trimmed = customerDiscount.trim();
      const discount =
        trimmed === "" ? null : Number.parseFloat(trimmed);
      if (discount != null && (Number.isNaN(discount) || discount < 0 || discount > 100)) {
        throw new Error("invalid-discount");
      }
      return updateBusinessCustomerPricingOverrides(workspace, connectionId, {
        customerDiscountPercent: discount,
        categoryOverrides,
      });
    },
    onSuccess: (data) => {
      void queryClient.invalidateQueries({
        queryKey: ["connected-commerce", "pricing", connectionId],
      });
      void queryClient.invalidateQueries({
        queryKey: ["connected-suppliers", "business-customer", connectionId],
      });
      setCustomerDiscount(
        data.customerDiscountPercent == null ? "" : String(data.customerDiscountPercent),
      );
      setCategoryOverrides(data.categoryOverrides.map((r) => ({ ...r })));
      showToast({ tone: "success", title: t("customers.business.connectedCommerce.saved") });
    },
    onError: (error) => {
      if (error instanceof Error && error.message === "invalid-discount") {
        showToast({
          tone: "error",
          title: t("customers.business.connectedCommerce.invalidDiscount"),
        });
        return;
      }
      showToast({
        tone: "error",
        title:
          error instanceof PosApiError && error.problem.detail
            ? error.problem.detail
            : t("customers.business.connectedCommerce.saveFailed"),
      });
    },
  });

  const loading = timingQuery.isLoading || pricingQuery.isLoading || orgSettingsQuery.isLoading;
  const loadError = timingQuery.isError || pricingQuery.isError;

  function orgAllows(code: TimingCode): boolean {
    if (!org) {
      return true;
    }
    const option = TIMING_OPTIONS.find((o) => o.code === code);
    return option ? org[option.orgFlag] : false;
  }

  function customerAllows(code: TimingCode): boolean {
    switch (code) {
      case "PayBeforeFulfillment":
        return allowBefore;
      case "PayOnDeliveryOrReceipt":
        return allowOnDelivery;
      case "SupplierCredit":
        return allowCredit;
    }
  }

  function setCustomerAllows(code: TimingCode, next: boolean) {
    switch (code) {
      case "PayBeforeFulfillment":
        setAllowBefore(next);
        break;
      case "PayOnDeliveryOrReceipt":
        setAllowOnDelivery(next);
        break;
      case "SupplierCredit":
        setAllowCredit(next);
        break;
    }
  }

  const enabledDefaults = TIMING_OPTIONS.filter(
    (o) => orgAllows(o.code) && (useOrgDefaults || customerAllows(o.code)),
  );

  useEffect(() => {
    const enabledCodes = TIMING_OPTIONS.filter(
      (o) => orgAllows(o.code) && (useOrgDefaults || customerAllows(o.code)),
    ).map((o) => o.code);
    if (enabledCodes.length === 0) {
      return;
    }
    if (!enabledCodes.includes(defaultTiming)) {
      setDefaultTiming(enabledCodes[0]!);
    }
  }, [allowBefore, allowOnDelivery, allowCredit, useOrgDefaults, org, defaultTiming]);

  return (
    <div
      className="grid grid-cols-1 gap-3 lg:grid-cols-2 lg:items-start"
      data-testid="business-customer-connected-commerce"
    >
      <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="business-customer-payment-timing">
        <div className="min-w-0">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("customers.business.connectedCommerce.paymentTimingTitle")}
          </h2>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.business.connectedCommerce.paymentTimingHelp")}
          </p>
        </div>

        {loading ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">…</p>
        ) : loadError ? (
          <Notice tone="danger">{t("customers.business.connectedCommerce.loadFailed")}</Notice>
        ) : (
          <>
            <BranchFulfillmentSwitch
              checked={useOrgDefaults}
              disabled={!canManage || timingMutation.isPending}
              pending={timingMutation.isPending}
              label={t("customers.business.connectedCommerce.useOrgDefaults")}
              hint={
                useOrgDefaults
                  ? t("customers.business.connectedCommerce.useOrgDefaultsOn")
                  : t("customers.business.connectedCommerce.useOrgDefaultsOff")
              }
              testId="business-payment-timing-use-org"
              onCheckedChange={setUseOrgDefaults}
            />

            <Notice tone="info">
              {t("customers.business.connectedCommerce.overrideNoAlert")}
            </Notice>

            <div className="grid grid-cols-3 gap-2">
              {TIMING_OPTIONS.map((option) => {
                const globallyOff = !orgAllows(option.code);
                const checked = useOrgDefaults
                  ? Boolean(timingQuery.data?.[option.effectiveFlag])
                  : customerAllows(option.code);
                return (
                  <label
                    key={option.code}
                    className={`flex min-w-0 flex-col gap-2 rounded-md border border-border px-3 py-2 ${
                      globallyOff ? "opacity-60" : ""
                    }`}
                  >
                    <span className="flex items-start gap-2">
                      <input
                        type="checkbox"
                        className="mt-1 shrink-0"
                        checked={checked}
                        disabled={
                          !canManage ||
                          timingMutation.isPending ||
                          useOrgDefaults ||
                          globallyOff
                        }
                        onChange={(e) => setCustomerAllows(option.code, e.target.checked)}
                        data-testid={`business-payment-timing-${option.code}`}
                      />
                      <span className="min-w-0 flex-1 text-[length:var(--exits-text-sm)] font-medium">
                        {t(option.labelKey)}
                        {globallyOff ? (
                          <span className="mt-0.5 block text-[length:var(--exits-text-xs)] font-normal text-muted">
                            {t("customers.business.connectedCommerce.timingDisabledGlobally")}
                          </span>
                        ) : null}
                      </span>
                    </span>
                    {timingQuery.data?.[option.effectiveFlag] ? (
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

            {!useOrgDefaults ? (
              <div className="flex flex-col gap-1.5">
                <span className="text-[length:var(--exits-text-sm)] font-normal">
                  {t("connectedCommerce.defaultTiming")}
                </span>
                <ExitsPillSelect
                  aria-label={t("connectedCommerce.defaultTiming")}
                  value={
                    enabledDefaults.some((option) => option.code === defaultTiming)
                      ? defaultTiming
                      : (enabledDefaults[0]?.code ?? defaultTiming)
                  }
                  onChange={setDefaultTiming}
                  disabled={!canManage || timingMutation.isPending || enabledDefaults.length === 0}
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
                  testId="business-payment-timing-default"
                />
              </div>
            ) : (
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("customers.business.connectedCommerce.effectiveDefault").replace(
                  "{timing}",
                  t(timingLabelKey(timingQuery.data?.effectiveDefaultPaymentTiming)),
                )}
              </p>
            )}

            <div className="mt-auto grid w-full grid-cols-2 gap-2">
              {canManage ? (
                <Button
                  type="button"
                  className="w-full"
                  disabled={!online || timingMutation.isPending}
                  onClick={() => timingMutation.mutate()}
                  data-testid="business-payment-timing-save"
                >
                  {t("connectedCommerce.save")}
                </Button>
              ) : (
                <span aria-hidden className="invisible" />
              )}
              <Button type="button" appearance="outline" size="default" className="w-full" asChild>
                <Link to="/org/connected-commerce?tab=payments">
                  {t("customers.business.connectedCommerce.openOrgSettings")}
                </Link>
              </Button>
            </div>
          </>
        )}
      </Card>

      <Card className="flex min-w-0 flex-col gap-3 p-3" data-testid="business-customer-pricing-overrides">
        <div className="min-w-0">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("customers.business.connectedCommerce.pricingTitle")}
          </h2>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.business.connectedCommerce.pricingHelp")}
          </p>
        </div>

        {org ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.business.connectedCommerce.orgDefaultDiscount").replace(
              "{n}",
              String(org.defaultB2bDiscountPercent),
            )}
          </p>
        ) : null}

        <Input
          label={t("customers.business.connectedCommerce.customerDefaultDiscount")}
          type="number"
          min={0}
          max={100}
          step="0.01"
          placeholder={t("customers.business.connectedCommerce.inheritPlaceholder")}
          value={customerDiscount}
          disabled={!canManage || pricingMutation.isPending}
          onChange={(e) => setCustomerDiscount(e.target.value)}
          data-testid="business-pricing-customer-discount"
        />

        <div className="flex flex-wrap items-center justify-between gap-3 border-t border-border pt-3">
          <div className="min-w-0 flex-1">
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("customers.business.connectedCommerce.categoryOverrides")}
            </p>
            <p className="m-0 mt-0.5 text-[length:var(--exits-text-xs)] text-muted">
              {categoryOverrides.length === 0
                ? t("customers.business.connectedCommerce.noCategoryOverrides")
                : t("customers.business.connectedCommerce.categoryOverridesCount").replace(
                    "{n}",
                    String(categoryOverrides.length),
                  )}
            </p>
          </div>
          <Button
            type="button"
            appearance="outline"
            size="default"
            className="w-auto shrink-0"
            disabled={!canManage || pricingMutation.isPending}
            onClick={() => setCategoryOverridesOpen(true)}
            data-testid="business-pricing-open-category-overrides"
          >
            {categoryOverrides.length === 0
              ? t("connectedCommerce.pricingOverride.add")
              : t("customers.business.connectedCommerce.manageCategoryOverrides")}
          </Button>
        </div>

        <BottomSheet
          open={categoryOverridesOpen}
          onClose={() => setCategoryOverridesOpen(false)}
          title={t("customers.business.connectedCommerce.categoryOverrides")}
          panelId="business-pricing-category-overrides-sheet"
          testId="business-pricing-category-overrides-sheet"
          closeLabel={t("connectedCommerce.pricingOverride.cancel")}
          presentation="sheet-mobile-dialog-desktop"
          panelClassName="md:w-[min(100%-2rem,36rem)] md:max-h-[85vh]"
        >
          <CategoryPricingOverridesPanel
            rules={categoryOverrides}
            categories={(categoriesQuery.data?.items ?? []).map((c) => ({
              categoryId: c.categoryId,
              name: c.name,
            }))}
            canEdit={canManage && !pricingMutation.isPending}
            onChange={setCategoryOverrides}
            showHeader={false}
            nested
            hierarchyHelp={t("customers.business.connectedCommerce.pricingHelp")}
            emptyTitle={t("customers.business.connectedCommerce.categoryOverrideEmptyTitle")}
            emptyDetail={t("customers.business.connectedCommerce.noCategoryOverrides")}
            removeDetail={t("customers.business.connectedCommerce.categoryOverrideRemoveDetail")}
          />
        </BottomSheet>

        {canManage ? (
          <Button
            type="button"
            className="w-auto self-end"
            disabled={!online || pricingMutation.isPending}
            onClick={() => pricingMutation.mutate()}
            data-testid="business-pricing-save"
          >
            {t("connectedCommerce.save")}
          </Button>
        ) : null}
      </Card>
    </div>
  );
}

function timingLabelKey(code: string | undefined): MessageKey {
  switch (code) {
    case "PayOnDeliveryOrReceipt":
      return "connectedCommerce.timing.payOnDelivery";
    case "SupplierCredit":
      return "connectedCommerce.timing.supplierCredit";
    default:
      return "connectedCommerce.timing.payBefore";
  }
}
