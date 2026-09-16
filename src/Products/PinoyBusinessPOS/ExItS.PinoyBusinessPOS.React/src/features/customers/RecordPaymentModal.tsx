import { useEffect, useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Equal, Wallet } from "lucide-react";
import {
  createCustomerRepayment,
  type CreatePosRepaymentInput,
  type UtangRepaymentPaymentMethod,
  UTANG_REPAYMENT_PAYMENT_METHODS,
} from "@/api/pos/pos-customers-client";
import { createBusinessCustomerRepayment } from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { Notice } from "@/components/exits/Notice";
import { EXITS_CANCEL_BUTTON_CLASS } from "@/components/exits/exits-cancel-button";
import { Button, buttonIconMotion } from "@/components/ui/button";
import { useToast } from "@/components/exits/ToastProvider";
import { useI18n } from "@/i18n/I18nProvider";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";

type SharedRecordPaymentModalProps = {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  customerKind: "personal" | "business";
  displayName: string;
  outstandingBalance: number;
  onSuccess: () => void;
};

type PersonalRecordPaymentModalProps = SharedRecordPaymentModalProps & {
  customerKind: "personal";
  customerId: string;
  connectionId?: never;
};

type BusinessRecordPaymentModalProps = SharedRecordPaymentModalProps & {
  customerKind: "business";
  connectionId: string;
  customerId?: never;
};

export type RecordPaymentModalProps =
  | PersonalRecordPaymentModalProps
  | BusinessRecordPaymentModalProps;

function methodLabelKey(method: UtangRepaymentPaymentMethod) {
  switch (method) {
    case "ManualGCash":
      return "customers.paymentMethod.manualGcash";
    case "Check":
      return "customers.paymentMethod.check";
    default:
      return "customers.paymentMethod.cash";
  }
}

export function RecordPaymentModal(props: RecordPaymentModalProps) {
  const { t } = useI18n();
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const workspace = usePosWorkspaceScope();

  const [amountText, setAmountText] = useState("");
  const [remarks, setRemarks] = useState("");
  const [paymentMethod, setPaymentMethod] = useState<UtangRepaymentPaymentMethod>("Cash");
  const [checkNumber, setCheckNumber] = useState("");
  const [bankName, setBankName] = useState("");
  const [checkDate, setCheckDate] = useState("");
  const [accountName, setAccountName] = useState("");
  const [reference, setReference] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  useEffect(() => {
    if (!props.open) {
      return;
    }
    setAmountText("");
    setRemarks("");
    setPaymentMethod("Cash");
    setCheckNumber("");
    setBankName("");
    setCheckDate("");
    setAccountName("");
    setReference("");
    setFormError(null);
  }, [props.open]);

  const amount = useMemo(() => parseMoneyAmountInput(amountText), [amountText]);
  const exceedsOutstanding =
    amount != null && amount - props.outstandingBalance > 1e-9;
  const exceedsOutstandingMessage = exceedsOutstanding
    ? t("customers.paymentExceedsOutstanding").replace(
        "{max}",
        `₱${formatMoneyAmountInput(props.outstandingBalance)}`,
      )
    : null;
  const remainingBalance =
    amount == null || exceedsOutstanding
      ? null
      : Math.max(0, Math.round((props.outstandingBalance - amount) * 100) / 100);
  const isCheck = paymentMethod === "Check";

  const mutation = useMutation({
    mutationFn: async () => {
      if (!workspace) {
        throw new Error(t("session.loading"));
      }
      if (amount == null || amount <= 0) {
        throw new Error(t("customers.paymentInvalid"));
      }
      if (amount - props.outstandingBalance > 1e-9) {
        throw new Error(
          t("customers.paymentExceedsOutstanding").replace(
            "{max}",
            `₱${formatMoneyAmountInput(props.outstandingBalance)}`,
          ),
        );
      }
      if (isCheck && (!checkNumber.trim() || !bankName.trim() || !checkDate.trim())) {
        throw new Error(t("customers.checkFieldsRequired"));
      }

      const payload: CreatePosRepaymentInput = {
        amount,
        remarks,
        paymentMethod,
        checkNumber,
        bankName,
        checkDate,
        accountName,
        reference,
      };

      if (props.customerKind === "personal") {
        await createCustomerRepayment(workspace, props.customerId, payload);
      } else {
        await createBusinessCustomerRepayment(workspace, props.connectionId, payload);
      }
    },
    onSuccess: async () => {
      if (!workspace) {
        return;
      }
      if (props.customerKind === "personal") {
        await queryClient.invalidateQueries({
          queryKey: ["customers", "credit-summary", workspace.organizationId, props.customerId],
        });
        await queryClient.invalidateQueries({
          queryKey: ["customers", "credit-policy", workspace.organizationId, props.customerId],
        });
        await queryClient.invalidateQueries({
          queryKey: ["customers", "repayments", workspace.organizationId, props.customerId],
        });
      } else {
        await queryClient.invalidateQueries({
          queryKey: ["business-customers", "utang-summary", workspace.organizationId, props.connectionId],
        });
        await queryClient.invalidateQueries({
          queryKey: ["business-customers", "credit-policy", workspace.organizationId, props.connectionId],
        });
        await queryClient.invalidateQueries({
          queryKey: ["business-customers", "repayments", workspace.organizationId, props.connectionId],
        });
        await queryClient.invalidateQueries({ queryKey: ["supplier-payables"] });
        await queryClient.invalidateQueries({ queryKey: ["supplier-payable-summary"] });
        await queryClient.invalidateQueries({ queryKey: ["connected-suppliers"] });
      }

      const formatted = formatMoneyAmountInput(amount ?? 0);
      if (paymentMethod === "Check") {
        showToast(
          t("customers.recordedPaymentCheckToast").replace("{amount}", `₱${formatted}`),
          "success",
        );
      } else {
        showToast(
          t("customers.recordedPaymentCashToast").replace("{amount}", `₱${formatted}`),
          "success",
        );
      }
      props.onOpenChange(false);
      props.onSuccess();
    },
    onError: (error) => {
      if (error instanceof PosApiError) {
        setFormError(error.problem.detail ?? error.message);
      } else {
        setFormError(error instanceof Error ? error.message : t("error.detail"));
      }
    },
  });

  const canSubmit =
    !mutation.isPending &&
    props.outstandingBalance > 0 &&
    amount != null &&
    amount > 0 &&
    !exceedsOutstanding &&
    (!isCheck || Boolean(checkNumber.trim() && bankName.trim() && checkDate.trim()));

  if (!props.open) {
    return null;
  }

  return (
    <ExitsModal
      open={props.open}
      onOpenChange={props.onOpenChange}
      title={`${t("customers.recordPayment")} - ${props.displayName}`}
      busy={mutation.isPending}
      testId="record-payment-modal"
      closeLabel={t("customers.creditPolicy.cancel")}
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            className={EXITS_CANCEL_BUTTON_CLASS}
            onClick={() => props.onOpenChange(false)}
            disabled={mutation.isPending}
            data-testid="record-payment-cancel"
          >
            {t("customers.creditPolicy.cancel")}
          </Button>
          <Button
            type="button"
            variant="default"
            disabled={!canSubmit}
            onClick={() => {
              setFormError(null);
              mutation.mutate();
            }}
            data-testid="record-payment-submit"
          >
            <Wallet className="size-4 shrink-0" aria-hidden />
            {mutation.isPending ? t("customers.saving") : t("customers.recordPayment")}
          </Button>
        </>
      }
    >
      <div className="grid gap-3">
          <div className="border-t border-border pt-3">
            <Notice
              tone="info"
              icon={null}
              testId="record-payment-amount-owed"
              className="px-2.5 py-1.5 text-[length:var(--exits-text-md)] font-semibold"
            >
              {`${t("customers.amountOwed")}: ₱${formatMoneyAmountInput(props.outstandingBalance)}`}
            </Notice>
          </div>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("customers.payment")}
            <span className="checkout-cash-received-row">
              <input
                type="text"
                inputMode="decimal"
                className="checkout-cash-received-row__input min-w-0 flex-1 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 tabular-nums"
                value={amountText}
                disabled={mutation.isPending || props.outstandingBalance <= 0}
                onChange={(event) => {
                  setFormError(null);
                  setAmountText(normalizeMoneyAmountTyping(event.target.value));
                }}
                onBlur={() => {
                  if (amount !== null) {
                    setAmountText(formatMoneyAmountInput(amount));
                  }
                }}
                data-testid="record-payment-amount"
              />
              <Button
                type="button"
                variant="outline"
                className="checkout-cash-received-row__exact shrink-0"
                data-testid="record-payment-exact"
                disabled={mutation.isPending || props.outstandingBalance <= 0}
                onClick={() => {
                  setFormError(null);
                  setAmountText(formatMoneyAmountInput(props.outstandingBalance));
                }}
              >
                <Equal className={`size-4 shrink-0 ${buttonIconMotion.view}`} aria-hidden />
                {t("checkout.cashExact")}
              </Button>
            </span>
            {exceedsOutstandingMessage ? (
              <Notice
                tone="warning"
                testId="record-payment-exceeds-warning"
                className="px-2.5 py-1.5 text-[length:var(--exits-text-xs)]"
              >
                {exceedsOutstandingMessage}
              </Notice>
            ) : null}
          </label>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("customers.paymentMethod")}
            <ExitsSelect
              value={paymentMethod}
              options={UTANG_REPAYMENT_PAYMENT_METHODS.map((method) => ({
                value: method,
                label: t(methodLabelKey(method)),
              }))}
              onChange={setPaymentMethod}
              menuLabel={t("customers.paymentMethod")}
              testId="record-payment-method"
            />
          </label>

          {isCheck ? (
            <>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("customers.checkNumber")}
                <input
                  className="rounded-md border border-border bg-background px-3"
                  value={checkNumber}
                  onChange={(event) => setCheckNumber(event.target.value)}
                  data-testid="record-payment-check-number"
                />
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("customers.bankName")}
                <input
                  className="rounded-md border border-border bg-background px-3"
                  value={bankName}
                  onChange={(event) => setBankName(event.target.value)}
                  data-testid="record-payment-bank-name"
                />
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("customers.checkDate")}
                <input
                  type="date"
                  className="rounded-md border border-border bg-background px-3"
                  value={checkDate}
                  onChange={(event) => setCheckDate(event.target.value)}
                  data-testid="record-payment-check-date"
                />
              </label>
              <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
                {t("customers.accountName")}
                <input
                  className="rounded-md border border-border bg-background px-3"
                  value={accountName}
                  onChange={(event) => setAccountName(event.target.value)}
                  data-testid="record-payment-account-name"
                />
              </label>
            </>
          ) : null}

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("customers.reference")}
            <input
              className="rounded-md border border-border bg-background px-3"
              value={reference}
              onChange={(event) => setReference(event.target.value)}
              data-testid="record-payment-reference"
            />
          </label>

          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("customers.paymentNotes")}
            <textarea
              className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
              value={remarks}
              onChange={(event) => setRemarks(event.target.value)}
              data-testid="record-payment-remarks"
            />
          </label>

          {remainingBalance !== null ? (
            <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="record-payment-remaining-balance">
              {t("customers.remainingBalance")}: ₱{formatMoneyAmountInput(remainingBalance)}
            </p>
          ) : null}
        </div>

        {formError ? (
          <p className="m-0 mt-3 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
            {formError}
          </p>
        ) : null}
    </ExitsModal>
  );
}
