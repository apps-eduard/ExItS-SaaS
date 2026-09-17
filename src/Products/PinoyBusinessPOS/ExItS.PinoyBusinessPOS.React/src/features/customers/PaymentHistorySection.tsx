import { useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  bounceBusinessRepaymentCheck,
  cancelBusinessRepaymentCheck,
  clearBusinessRepaymentCheck,
  listBusinessCustomerRepayments,
} from "@/api/pos/pos-connected-suppliers-client";
import {
  bounceCheckRepayment,
  cancelCheckRepayment,
  clearCheckRepayment,
  listCustomerRepayments,
} from "@/api/pos/pos-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { ConfirmActionDialog } from "@/components/exits/ConfirmActionDialog";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { StatusChip } from "@/components/exits/StatusChip";
import { useToast } from "@/components/exits/ToastProvider";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

type SharedProps = {
  customerKind: "personal" | "business";
  canManageChecks: boolean;
  online: boolean;
};

type PersonalProps = SharedProps & {
  customerKind: "personal";
  customerId: string;
  connectionId?: never;
};

type BusinessProps = SharedProps & {
  customerKind: "business";
  connectionId: string;
  customerId?: never;
};

export type PaymentHistorySectionProps = PersonalProps | BusinessProps;

type PendingAction = {
  kind: "clear" | "bounce" | "cancel";
  repaymentId: string;
};

function clearingTone(status: string) {
  switch (status) {
    case "Cleared":
      return "success";
    case "Bounced":
      return "danger";
    case "Cancelled":
      return "warning";
    case "PendingClearing":
      return "info";
    default:
      return "default";
  }
}

function clearingStatusLabelKey(status: string) {
  switch (status) {
    case "PendingClearing":
      return "customers.checkStatus.pending";
    case "Cleared":
      return "customers.checkStatus.cleared";
    case "Bounced":
      return "customers.checkStatus.bounced";
    case "Cancelled":
      return "customers.checkStatus.cancelled";
    default:
      return "customers.checkStatus.none";
  }
}

function methodLabelKey(method: string) {
  switch (method) {
    case "ManualGCash":
      return "customers.paymentMethod.manualGcash";
    case "Check":
      return "customers.paymentMethod.check";
    default:
      return "customers.paymentMethod.cash";
  }
}

export function PaymentHistorySection(props: PaymentHistorySectionProps) {
  const { t } = useI18n();
  const workspace = usePosWorkspaceScope();
  const queryClient = useQueryClient();
  const { showToast } = useToast();
  const [pendingAction, setPendingAction] = useState<PendingAction | null>(null);

  const queryKey = useMemo(() => {
    if (!workspace) {
      return [];
    }
    return props.customerKind === "personal"
      ? ["customers", "repayments", workspace.organizationId, props.customerId]
      : ["business-customers", "repayments", workspace.organizationId, props.connectionId];
  }, [props.connectionId, props.customerId, props.customerKind, workspace]);

  const repaymentsQuery = useQuery({
    queryKey,
    enabled: Boolean(workspace) && props.online && queryKey.length > 0,
    queryFn: ({ signal }) =>
      props.customerKind === "personal"
        ? listCustomerRepayments(workspace!, props.customerId, { page: 1, pageSize: 50 }, signal)
        : listBusinessCustomerRepayments(
            workspace!,
            props.connectionId,
            { page: 1, pageSize: 50 },
            signal,
          ),
  });

  const actionMutation = useMutation({
    mutationFn: async (action: PendingAction) => {
      if (!workspace) {
        throw new Error(t("session.loading"));
      }
      if (props.customerKind === "personal") {
        if (action.kind === "clear") {
          await clearCheckRepayment(workspace, action.repaymentId);
        } else if (action.kind === "bounce") {
          await bounceCheckRepayment(workspace, action.repaymentId);
        } else {
          await cancelCheckRepayment(workspace, action.repaymentId);
        }
      } else if (action.kind === "clear") {
        await clearBusinessRepaymentCheck(workspace, action.repaymentId);
      } else if (action.kind === "bounce") {
        await bounceBusinessRepaymentCheck(workspace, action.repaymentId);
      } else {
        await cancelBusinessRepaymentCheck(workspace, action.repaymentId);
      }
    },
    onSuccess: async () => {
      if (!workspace) {
        return;
      }
      await queryClient.invalidateQueries({ queryKey });
      if (props.customerKind === "personal") {
        await queryClient.invalidateQueries({
          queryKey: ["customers", "credit-summary", workspace.organizationId, props.customerId],
        });
      } else {
        await queryClient.invalidateQueries({
          queryKey: ["business-customers", "utang-summary", workspace.organizationId, props.connectionId],
        });
      }
      setPendingAction(null);
      showToast(t("customers.checkStatusUpdated"), "success");
    },
    onError: (error) => {
      setPendingAction(null);
      showToast(
        error instanceof PosApiError
          ? (error.problem.detail ?? error.message)
          : t("customers.checkStatusUpdateFailed"),
        "error",
      );
    },
  });

  if (!workspace) {
    return null;
  }

  const items = repaymentsQuery.data?.items ?? [];

  return (
    <>
      <Card className="flex flex-col gap-3 p-4" data-testid="customer-payment-history">
        <div className="flex items-center justify-between gap-2">
          <h2 className="m-0 text-[length:var(--exits-text-md)] font-semibold">
            {t("customers.paymentsTitle")}
          </h2>
        </div>
        {!props.online ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("connectivity.actionRequiresInternet")}
          </p>
        ) : null}
        {repaymentsQuery.isLoading ? <LoadingState label={t("loading.label")} /> : null}
        {repaymentsQuery.isError ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {(repaymentsQuery.error as Error).message}
          </p>
        ) : null}
        {repaymentsQuery.isSuccess && items.length === 0 ? (
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.paymentsEmptyDetail")}
          </p>
        ) : null}
        {items.length > 0 ? (
          <ul className="m-0 flex list-none flex-col gap-3 p-0" data-testid="customer-payment-history-list">
            {items.map((payment) => {
              const isPendingCheck = payment.checkClearingStatus === "PendingClearing";
              const isCheck = payment.paymentMethod === "Check";
              return (
                <li
                  key={payment.repaymentId}
                  className="rounded-[var(--exits-radius-md)] border border-border p-3"
                  data-testid={`customer-payment-history-${payment.repaymentId}`}
                >
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <p className="m-0 font-semibold tabular-nums">
                      <MoneyDisplay amount={payment.amount} />
                    </p>
                    <div className="flex flex-wrap items-center gap-2">
                      {isCheck ? (
                        <StatusChip tone={clearingTone(payment.checkClearingStatus)}>
                          {t(clearingStatusLabelKey(payment.checkClearingStatus))}
                        </StatusChip>
                      ) : null}
                      <StatusChip tone={payment.status === "Active" ? "success" : "warning"}>
                        {payment.status}
                      </StatusChip>
                    </div>
                  </div>

                  <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                    {t(methodLabelKey(payment.paymentMethod))} ·{" "}
                    {new Date(payment.recordedAtUtc).toLocaleString()}
                  </p>
                  {payment.remarks?.trim() ? (
                    <p className="m-0 mt-1 text-[length:var(--exits-text-sm)]">
                      {payment.remarks}
                    </p>
                  ) : null}
                  {isCheck ? (
                    <dl className="m-0 mt-2 grid gap-1 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
                      <div>
                        <dt className="text-muted">{t("customers.checkNumber")}</dt>
                        <dd className="m-0">{payment.checkNumber?.trim() || "—"}</dd>
                      </div>
                      <div>
                        <dt className="text-muted">{t("customers.bankName")}</dt>
                        <dd className="m-0">{payment.bankName?.trim() || "—"}</dd>
                      </div>
                      <div>
                        <dt className="text-muted">{t("customers.checkDate")}</dt>
                        <dd className="m-0">{payment.checkDate?.trim() || "—"}</dd>
                      </div>
                      <div>
                        <dt className="text-muted">{t("customers.reference")}</dt>
                        <dd className="m-0">{payment.reference?.trim() || "—"}</dd>
                      </div>
                    </dl>
                  ) : null}

                  {isPendingCheck && props.canManageChecks ? (
                    <div className="mt-3 flex flex-wrap gap-2">
                      <Button
                        type="button"
                        variant="success"
                        onClick={() =>
                          setPendingAction({ kind: "clear", repaymentId: payment.repaymentId })
                        }
                        data-testid={`customer-payment-clear-${payment.repaymentId}`}
                      >
                        {t("customers.markCheckCleared")}
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        onClick={() =>
                          setPendingAction({ kind: "bounce", repaymentId: payment.repaymentId })
                        }
                        data-testid={`customer-payment-bounce-${payment.repaymentId}`}
                      >
                        {t("customers.markCheckBounced")}
                      </Button>
                      <Button
                        type="button"
                        variant="outline"
                        className="border-[color-mix(in_srgb,var(--exits-danger)_40%,var(--exits-border))] text-[var(--exits-danger)]"
                        onClick={() =>
                          setPendingAction({ kind: "cancel", repaymentId: payment.repaymentId })
                        }
                        data-testid={`customer-payment-cancel-check-${payment.repaymentId}`}
                      >
                        {t("customers.cancelCheck")}
                      </Button>
                    </div>
                  ) : null}
                </li>
              );
            })}
          </ul>
        ) : null}
      </Card>

      <ConfirmActionDialog
        open={pendingAction != null}
        variant={pendingAction?.kind === "clear" ? "default" : "danger"}
        title={
          pendingAction?.kind === "clear"
            ? t("customers.clearCheckConfirmTitle")
            : pendingAction?.kind === "bounce"
              ? t("customers.bounceCheckConfirmTitle")
              : t("customers.cancelCheckConfirmTitle")
        }
        description={
          pendingAction?.kind === "clear"
            ? t("customers.clearCheckConfirmDetail")
            : pendingAction?.kind === "bounce"
              ? t("customers.bounceCheckConfirmDetail")
              : t("customers.cancelCheckConfirmDetail")
        }
        confirmLabel={
          pendingAction?.kind === "clear"
            ? t("customers.markCheckCleared")
            : pendingAction?.kind === "bounce"
              ? t("customers.markCheckBounced")
              : t("customers.cancelCheck")
        }
        cancelLabel={t("customers.creditPolicy.cancel")}
        pending={actionMutation.isPending}
        onCancel={() => setPendingAction(null)}
        onConfirm={() => {
          if (pendingAction) {
            actionMutation.mutate(pendingAction);
          }
        }}
        testId="customer-payment-check-action-confirm"
      />
    </>
  );
}
