import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CreditCard } from "lucide-react";
import {
  listPaymentMethods,
  upsertPaymentMethod,
  type PaymentMethodSettingDto,
} from "@/api/pos/pos-payment-methods-client";
import {
  listBranchManagementSummaries,
  type BranchManagementSummaryItemDto,
} from "@/api/platform/organization-branches-client";
import { canManagePaymentMethods, canUseOnlinePayments } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { normalizeBranchStatusFilter } from "@/features/branches/branch-code";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { pageBackNav } from "@/navigation/page-back-nav";
import { usePageSmartBack } from "@/navigation/useSmartBack";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function statusLabel(method: PaymentMethodSettingDto, t: (k: MessageKey) => string): string {
  if (method.comingSoon) return t("paymentMethods.comingSoon");
  if (!method.entitled) return t("paymentMethods.disabled");
  if (method.availability === "BuiltIn") return t("paymentMethods.active");
  return method.isEnabled ? t("paymentMethods.enabled") : t("paymentMethods.disabled");
}

function isSelectedBranchesScope(scope: string): boolean {
  return scope.localeCompare("SelectedBranches", undefined, { sensitivity: "accent" }) === 0;
}

type ConfigurableMethodCardProps = {
  method: PaymentMethodSettingDto;
  branches: BranchManagementSummaryItemDto[];
  canManage: boolean;
  saving: boolean;
  onSave: (input: {
    methodCode: string;
    isEnabled: boolean;
    branchScope: "AllBranches" | "SelectedBranches";
    selectedBranchIds: string[];
    requireReference: boolean;
  }) => void;
};

function ConfigurableMethodCard({
  method,
  branches,
  canManage,
  saving,
  onSave,
}: ConfigurableMethodCardProps) {
  const { t } = useI18n();
  const [branchScope, setBranchScope] = useState<"AllBranches" | "SelectedBranches">(
    isSelectedBranchesScope(method.branchScope) ? "SelectedBranches" : "AllBranches",
  );
  const [selectedBranchIds, setSelectedBranchIds] = useState<string[]>(
    () => [...method.selectedBranchIds],
  );

  const canConfigure = canManage && method.canConfigure;
  const dirty =
    branchScope !== (isSelectedBranchesScope(method.branchScope) ? "SelectedBranches" : "AllBranches")
    || selectedBranchIds.length !== method.selectedBranchIds.length
    || selectedBranchIds.some((id) => !method.selectedBranchIds.includes(id));

  function toggleBranch(branchId: string) {
    setSelectedBranchIds((current) =>
      current.includes(branchId)
        ? current.filter((id) => id !== branchId)
        : [...current, branchId],
    );
  }

  return (
    <Card
      className="flex h-full min-w-0 flex-col gap-3 p-3"
      data-testid={`payment-method-card-${method.methodCode}`}
    >
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <div className="font-medium">{method.displayName ?? method.methodCode}</div>
          <div className="text-[length:var(--exits-text-xs)] text-muted">{statusLabel(method, t)}</div>
        </div>
        {canConfigure ? (
          <Button
            type="button"
            variant="outline"
            disabled={saving}
            data-testid={`payment-method-toggle-${method.methodCode}`}
            onClick={() =>
              onSave({
                methodCode: method.methodCode,
                isEnabled: !method.isEnabled,
                branchScope,
                selectedBranchIds:
                  branchScope === "SelectedBranches" ? selectedBranchIds : [],
                requireReference: method.requireReference,
              })
            }
          >
            {method.isEnabled ? t("paymentMethods.disabled") : t("paymentMethods.enabled")}
          </Button>
        ) : null}
      </div>

      {canConfigure ? (
        <div
          className="flex flex-col gap-2 border-t border-[color:var(--exits-border)] pt-2"
          data-testid={`payment-method-availability-${method.methodCode}`}
        >
          <div className="text-[length:var(--exits-text-xs)] font-semibold uppercase tracking-wide text-muted">
            {t("paymentMethods.availability")}
          </div>
          <div className="flex flex-col gap-1.5" role="radiogroup" aria-label={t("paymentMethods.availability")}>
            <label className="flex cursor-pointer items-center gap-2 text-[length:var(--exits-text-sm)]">
              <input
                type="radio"
                name={`branch-scope-${method.methodCode}`}
                checked={branchScope === "AllBranches"}
                data-testid={`payment-method-scope-all-${method.methodCode}`}
                onChange={() => setBranchScope("AllBranches")}
              />
              {t("paymentMethods.allBranches")}
            </label>
            <label className="flex cursor-pointer items-center gap-2 text-[length:var(--exits-text-sm)]">
              <input
                type="radio"
                name={`branch-scope-${method.methodCode}`}
                checked={branchScope === "SelectedBranches"}
                data-testid={`payment-method-scope-selected-${method.methodCode}`}
                onChange={() => setBranchScope("SelectedBranches")}
              />
              {t("paymentMethods.selectedBranches")}
            </label>
          </div>

          {branchScope === "SelectedBranches" ? (
            <div className="flex flex-col gap-1.5 pl-1" data-testid={`payment-method-branch-list-${method.methodCode}`}>
              {branches.length === 0 ? (
                <div className="text-[length:var(--exits-text-xs)] text-muted">
                  {t("paymentMethods.noBranches")}
                </div>
              ) : (
                branches.map((branch) => (
                  <label
                    key={branch.id}
                    className="flex cursor-pointer items-center gap-2 text-[length:var(--exits-text-sm)]"
                  >
                    <input
                      type="checkbox"
                      checked={selectedBranchIds.includes(branch.id)}
                      data-testid={`payment-method-branch-${method.methodCode}-${branch.id}`}
                      onChange={() => toggleBranch(branch.id)}
                    />
                    {branch.name}
                  </label>
                ))
              )}
            </div>
          ) : null}

          {dirty ? (
            <Button
              type="button"
              className="w-fit"
              disabled={saving || (branchScope === "SelectedBranches" && selectedBranchIds.length === 0)}
              data-testid={`payment-method-save-availability-${method.methodCode}`}
              onClick={() =>
                onSave({
                  methodCode: method.methodCode,
                  isEnabled: method.isEnabled,
                  branchScope,
                  selectedBranchIds:
                    branchScope === "SelectedBranches" ? selectedBranchIds : [],
                  requireReference: method.requireReference,
                })
              }
            >
              {t("paymentMethods.save")}
            </Button>
          ) : null}
        </div>
      ) : null}
    </Card>
  );
}

export function PaymentMethodsSettingsPage() {
  const { t } = useI18n();
  const smartBack = usePageSmartBack({
    fallback: pageBackNav.org.to,
    backLabel: t(pageBackNav.org.labelKey),
    backTestId: "page-header-back-payment-methods",
  });
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const { sessionGrant } = useWorkspace();
  const workspace = usePosWorkspaceScope();
  const canManage = canManagePaymentMethods(sessionGrant);
  const canOnline = canUseOnlinePayments(sessionGrant);

  const organizationId = workspace?.organizationId ?? null;
  const orgScope = useMemo(
    () => (organizationId ? { organizationId, branchId: null as string | null } : null),
    [organizationId],
  );

  const query = useQuery({
    queryKey: ["payment-methods", organizationId],
    enabled: Boolean(orgScope),
    queryFn: ({ signal }) => listPaymentMethods(orgScope!, signal),
  });

  const branchesQuery = useQuery({
    queryKey: ["branch-management-summary", organizationId, "payment-methods"],
    enabled: Boolean(organizationId && canManage),
    queryFn: async ({ signal }) => {
      const result = await listBranchManagementSummaries(organizationId!, signal);
      if (!result.ok) {
        throw new Error(result.body?.detail ?? t("branches.mgmt.loadError"));
      }
      return result.value;
    },
  });

  const activeBranches = useMemo(() => {
    const items = branchesQuery.data ?? [];
    return items.filter((branch) => normalizeBranchStatusFilter(branch.status) === "Active");
  }, [branchesQuery.data]);

  const saveMutation = useMutation({
    mutationFn: (input: {
      methodCode: string;
      isEnabled: boolean;
      branchScope: "AllBranches" | "SelectedBranches";
      selectedBranchIds: string[];
      requireReference: boolean;
    }) =>
      upsertPaymentMethod(orgScope!, input.methodCode, {
        isEnabled: input.isEnabled,
        requireReference: input.requireReference,
        branchScope: input.branchScope,
        selectedBranchIds: input.selectedBranchIds,
      }),
    onSuccess: async () => {
      showToast({ tone: "success", title: t("paymentMethods.saved") });
      await queryClient.invalidateQueries({ queryKey: ["payment-methods"] });
    },
    onError: (error) => {
      showToast({
        tone: "error",
        title: t("paymentMethods.errorTitle"),
        description: error instanceof Error ? error.message : undefined,
      });
    },
  });

  if (!orgScope) {
    return (
      <div className="exits-page flex flex-col gap-3" data-testid="payment-methods-missing-org">
        <PageHeader
          title={t("paymentMethods.title")}
          {...smartBack}
        />
        <ErrorState title={t("paymentMethods.errorTitle")} detail={t("paymentMethods.orgRequired")} />
      </div>
    );
  }

  if (query.isLoading) {
    return <LoadingState label={t("paymentMethods.loading")} />;
  }

  if (query.isError || !query.data) {
    return (
      <div className="exits-page flex flex-col gap-3" data-testid="payment-methods-error">
        <PageHeader
          title={t("paymentMethods.title")}
          {...smartBack}
        />
        <ErrorState title={t("paymentMethods.errorTitle")} detail={(query.error as Error)?.message} />
      </div>
    );
  }

  const builtIn = query.data.filter((m) => m.availability === "BuiltIn");
  const manual = query.data.filter((m) => m.availability === "Configurable");
  const online = query.data.filter((m) => m.availability === "ComingSoon");

  return (
    <div className="exits-page flex min-w-0 flex-col gap-3" data-testid="payment-methods-page">
      <PageHeader
        title={t("paymentMethods.title")}
        description={t("paymentMethods.lede")}
        {...smartBack}
      />

      {!canManage ? (
        <Notice tone="info" testId="payment-methods-upgrade-management">
          {t("paymentMethods.upgradeForManagement")}
        </Notice>
      ) : null}

      <section className="flex flex-col gap-2" data-testid="payment-methods-built-in">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{t("paymentMethods.builtIn")}</h2>
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          {builtIn.map((method) => (
            <Card key={method.methodCode} className="flex min-w-0 items-center justify-between gap-3 p-3">
              <div className="min-w-0">
                <div className="font-medium">{method.displayName ?? method.methodCode}</div>
                <div className="text-[length:var(--exits-text-xs)] text-muted">{statusLabel(method, t)}</div>
              </div>
            </Card>
          ))}
        </div>
      </section>

      <section className="flex flex-col gap-2" data-testid="payment-methods-manual">
        <div className="flex items-center gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{t("paymentMethods.manual")}</h2>
          <span className="rounded-sm bg-[color-mix(in_srgb,var(--exits-primary)_14%,transparent)] px-1.5 py-0.5 text-[length:var(--exits-text-xs)] font-semibold text-primary">
            {t("paymentMethods.proBadge")}
          </span>
        </div>
        {manual.length === 0 ? (
          <EmptyState align="center" icon={<CreditCard className="size-5" />} title={t("paymentMethods.manual")} />
        ) : (
          <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
            {manual.map((method) => (
              <ConfigurableMethodCard
                key={`${method.methodCode}:${method.isEnabled}:${method.branchScope}:${method.selectedBranchIds.join(",")}`}
                method={method}
                branches={activeBranches}
                canManage={canManage}
                saving={saveMutation.isPending}
                onSave={(input) => saveMutation.mutate(input)}
              />
            ))}
          </div>
        )}
      </section>

      <section className="flex flex-col gap-2" data-testid="payment-methods-online">
        <div className="flex items-center gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{t("paymentMethods.online")}</h2>
          <span className="rounded-sm bg-[color-mix(in_srgb,var(--exits-primary)_14%,transparent)] px-1.5 py-0.5 text-[length:var(--exits-text-xs)] font-semibold text-primary">
            {t("paymentMethods.proPlusBadge")}
          </span>
        </div>
        {!canOnline ? (
          <Notice tone="info" testId="payment-methods-upgrade-online">
            {t("paymentMethods.upgradeForOnline")}
          </Notice>
        ) : null}
        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2 lg:grid-cols-3">
          {online.map((method) => (
            <Card key={method.methodCode} className="flex min-w-0 items-center justify-between gap-3 p-3">
              <div className="min-w-0">
                <div className="font-medium">{method.displayName ?? method.methodCode}</div>
                <div className="text-[length:var(--exits-text-xs)] text-muted">
                  {canOnline ? t("paymentMethods.comingSoon") : t("paymentMethods.notConnected")}
                </div>
              </div>
            </Card>
          ))}
        </div>
      </section>
    </div>
  );
}
