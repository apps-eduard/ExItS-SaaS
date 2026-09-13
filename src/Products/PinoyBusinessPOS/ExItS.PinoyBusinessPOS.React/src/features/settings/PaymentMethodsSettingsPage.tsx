import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CreditCard } from "lucide-react";
import {
  listPaymentMethods,
  upsertPaymentMethod,
  type PaymentMethodSettingDto,
} from "@/api/pos/pos-payment-methods-client";
import { canManagePaymentMethods, canUseOnlinePayments } from "@/access/pos-capabilities";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { Notice } from "@/components/exits/Notice";
import { PageHeader } from "@/components/exits/PageHeader";
import { useToast } from "@/components/exits/ToastProvider";
import { BranchRequiredPanel } from "@/features/workspace/BranchRequiredPanel";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

function statusLabel(method: PaymentMethodSettingDto, t: (k: string) => string): string {
  if (method.comingSoon) return t("paymentMethods.comingSoon");
  if (!method.entitled) return t("paymentMethods.disabled");
  if (method.availability === "BuiltIn") return t("paymentMethods.active");
  return method.isEnabled ? t("paymentMethods.enabled") : t("paymentMethods.disabled");
}

export function PaymentMethodsSettingsPage() {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const { boundWorkspace, sessionGrant } = useWorkspace();
  const canManage = canManagePaymentMethods(sessionGrant);
  const canOnline = canUseOnlinePayments(sessionGrant);

  const workspace = boundWorkspace?.branchId
    ? { organizationId: boundWorkspace.organizationId, branchId: boundWorkspace.branchId }
    : null;

  const query = useQuery({
    queryKey: ["payment-methods", workspace?.organizationId],
    enabled: Boolean(workspace),
    queryFn: ({ signal }) => listPaymentMethods(workspace!, signal),
  });

  const saveMutation = useMutation({
    mutationFn: (input: { methodCode: string; isEnabled: boolean }) =>
      upsertPaymentMethod(workspace!, input.methodCode, {
        isEnabled: input.isEnabled,
        requireReference: true,
        branchScope: "AllBranches",
      }),
    onSuccess: async () => {
      showToast({ tone: "success", message: t("paymentMethods.saved") });
      await queryClient.invalidateQueries({ queryKey: ["payment-methods"] });
    },
  });

  if (!workspace) {
    return <BranchRequiredPanel title={t("paymentMethods.title")} />;
  }

  if (query.isLoading) {
    return <LoadingState label={t("paymentMethods.loading")} />;
  }

  if (query.isError || !query.data) {
    return (
      <div className="exits-page flex flex-col gap-3" data-testid="payment-methods-error">
        <PageHeader
          title={t("paymentMethods.title")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
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
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-payment-methods"
      />

      {!canManage ? (
        <Notice tone="info" testId="payment-methods-upgrade-management">
          {t("paymentMethods.upgradeForManagement")}
        </Notice>
      ) : null}

      <section className="flex flex-col gap-2" data-testid="payment-methods-built-in">
        <h2 className="m-0 text-[length:var(--exits-text-sm)] font-semibold">{t("paymentMethods.builtIn")}</h2>
        {builtIn.map((method) => (
          <Card key={method.methodCode} className="flex items-center justify-between gap-3 p-3">
            <div>
              <div className="font-medium">{method.displayName ?? method.methodCode}</div>
              <div className="text-[length:var(--exits-text-xs)] text-muted">{statusLabel(method, t)}</div>
            </div>
          </Card>
        ))}
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
          manual.map((method) => (
            <Card key={method.methodCode} className="flex flex-wrap items-center justify-between gap-3 p-3">
              <div>
                <div className="font-medium">{method.displayName ?? method.methodCode}</div>
                <div className="text-[length:var(--exits-text-xs)] text-muted">{statusLabel(method, t)}</div>
              </div>
              {canManage && method.canConfigure ? (
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  disabled={saveMutation.isPending}
                  data-testid={`payment-method-toggle-${method.methodCode}`}
                  onClick={() =>
                    saveMutation.mutate({ methodCode: method.methodCode, isEnabled: !method.isEnabled })
                  }
                >
                  {method.isEnabled ? t("paymentMethods.disabled") : t("paymentMethods.enabled")}
                </Button>
              ) : null}
            </Card>
          ))
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
        {online.map((method) => (
          <Card key={method.methodCode} className="flex items-center justify-between gap-3 p-3">
            <div>
              <div className="font-medium">{method.methodCode}</div>
              <div className="text-[length:var(--exits-text-xs)] text-muted">
                {canOnline ? t("paymentMethods.comingSoon") : t("paymentMethods.notConnected")}
              </div>
            </div>
          </Card>
        ))}
      </section>
    </div>
  );
}
