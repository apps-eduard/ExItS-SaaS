import { useEffect, useMemo, useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Equal, Wallet } from "lucide-react";
import {
  createCustomerRepayment,
  type CreatePosRepaymentInput,
  type UtangRepaymentPaymentMethod,
  UTANG_REPAYMENT_PAYMENT_METHODS,
} from "@/api/pos/pos-customers-client";
import {
  createBusinessCustomerRepayment,
  listBusinessCustomerReceivables,
  type BusinessReceivable,
} from "@/api/pos/pos-connected-suppliers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { ExitsSelect } from "@/components/exits/ExitsSelect";
import { Notice } from "@/components/exits/Notice";
import { EXITS_CANCEL_BUTTON_CLASS } from "@/components/exits/exits-cancel-button";
import { Button, buttonIconMotion } from "@/components/ui/button";
import { useToast } from "@/components/exits/ToastProvider";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";
import { usePosWorkspaceScope } from "@/workspace/use-pos-workspace-scope";
import {
  allocatePaymentAutomatically,
  validateManualAllocations,
  type AllocationPreviewLine,
  type OpenReceivableForAllocation,
} from "@/features/customers/business-payment-allocation";

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
  preselectedCreditEntryId?: never;
};

type BusinessRecordPaymentModalProps = SharedRecordPaymentModalProps & {
  customerKind: "business";
  connectionId: string;
  customerId?: never;
  preselectedCreditEntryId?: string | null;
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

function receivableSourceLabelKey(sourceType: string | null | undefined): MessageKey {
  const normalized = (sourceType ?? "").trim().toLowerCase();
  if (normalized === "po") {
    return "customers.receivables.source.po";
  }
  if (normalized === "directpurchase" || normalized === "direct") {
    return "customers.receivables.source.direct";
  }
  if (normalized === "sale") {
    return "customers.receivables.source.sale";
  }
  return "customers.receivables.source.other";
}

function toOpenReceivables(items: BusinessReceivable[]): OpenReceivableForAllocation[] {
  return items.map((item) => ({
    creditEntryId: item.creditEntryId,
    outstandingBalance: item.outstandingBalance,
    dueDate: item.dueDate,
    createdAtUtc: item.createdAtUtc,
    sourceType: item.sourceType,
    sourceReference: item.sourceReference,
  }));
}

function formatDueDate(value: string | null | undefined): string {
  if (!value) {
    return "—";
  }
  const trimmed = value.trim();
  if (/^\d{4}-\d{2}-\d{2}/.test(trimmed)) {
    return trimmed.slice(0, 10);
  }
  return trimmed;
}

function seedManualFromLines(
  open: OpenReceivableForAllocation[],
  lines: AllocationPreviewLine[],
): Record<string, string> {
  const byId = new Map(lines.map((line) => [line.creditEntryId, line.amount]));
  const next: Record<string, string> = {};
  for (const receivable of open) {
    const amount = byId.get(receivable.creditEntryId);
    next[receivable.creditEntryId] =
      amount != null && amount > 0 ? formatMoneyAmountInput(amount) : "";
  }
  return next;
}

function seedPreselectedManual(
  open: OpenReceivableForAllocation[],
  preselectedCreditEntryId: string,
  paymentAmount: number,
): Record<string, string> {
  const next: Record<string, string> = {};
  for (const receivable of open) {
    if (receivable.creditEntryId === preselectedCreditEntryId) {
      const apply = Math.round(Math.min(paymentAmount, receivable.outstandingBalance) * 100) / 100;
      next[receivable.creditEntryId] = apply > 0 ? formatMoneyAmountInput(apply) : "";
    } else {
      next[receivable.creditEntryId] = "";
    }
  }
  return next;
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
  const [allocationMode, setAllocationMode] = useState<"auto" | "manual">("auto");
  const [manualAmounts, setManualAmounts] = useState<Record<string, string>>({});
  const [preselectSeedActive, setPreselectSeedActive] = useState(false);

  const isBusiness = props.customerKind === "business";
  const preselectedCreditEntryId = isBusiness ? (props.preselectedCreditEntryId ?? null) : null;
  const connectionId = isBusiness ? props.connectionId : null;

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
    if (isBusiness && preselectedCreditEntryId) {
      setAllocationMode("manual");
      setPreselectSeedActive(true);
    } else {
      setAllocationMode("auto");
      setPreselectSeedActive(false);
    }
    setManualAmounts({});
  }, [props.open, isBusiness, preselectedCreditEntryId]);

  const receivablesQuery = useQuery({
    queryKey: [
      "business-customers",
      "receivables",
      workspace?.organizationId,
      connectionId,
    ],
    enabled: Boolean(props.open && isBusiness && workspace && connectionId),
    queryFn: ({ signal }) =>
      listBusinessCustomerReceivables(workspace!, connectionId!, signal),
  });

  const openReceivables = useMemo(
    () => toOpenReceivables(receivablesQuery.data ?? []).filter((item) => item.outstandingBalance > 0),
    [receivablesQuery.data],
  );

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

  useEffect(() => {
    if (!props.open || !isBusiness || allocationMode !== "manual" || !preselectSeedActive) {
      return;
    }
    if (!preselectedCreditEntryId || amount == null || amount <= 0 || openReceivables.length === 0) {
      return;
    }
    setManualAmounts(seedPreselectedManual(openReceivables, preselectedCreditEntryId, amount));
  }, [
    props.open,
    isBusiness,
    allocationMode,
    preselectSeedActive,
    preselectedCreditEntryId,
    amount,
    openReceivables,
  ]);

  const autoPreviewLines = useMemo(() => {
    if (!isBusiness || amount == null || amount <= 0 || exceedsOutstanding) {
      return [] as AllocationPreviewLine[];
    }
    return allocatePaymentAutomatically(openReceivables, amount);
  }, [isBusiness, amount, exceedsOutstanding, openReceivables]);

  const manualAllocationResult = useMemo(() => {
    if (!isBusiness || allocationMode !== "manual" || amount == null || amount <= 0) {
      return null;
    }
    const allocations = openReceivables
      .map((receivable) => ({
        creditEntryId: receivable.creditEntryId,
        amount: parseMoneyAmountInput(manualAmounts[receivable.creditEntryId] ?? "") ?? 0,
      }))
      .filter((row) => row.amount > 0);
    return validateManualAllocations({
      open: openReceivables,
      allocations,
      paymentAmount: amount,
    });
  }, [isBusiness, allocationMode, amount, openReceivables, manualAmounts]);

  const previewLines: AllocationPreviewLine[] =
    !isBusiness || amount == null || amount <= 0 || exceedsOutstanding
      ? []
      : allocationMode === "auto"
        ? autoPreviewLines
        : manualAllocationResult?.ok
          ? manualAllocationResult.lines
          : openReceivables
              .map((receivable) => {
                const applied =
                  parseMoneyAmountInput(manualAmounts[receivable.creditEntryId] ?? "") ?? 0;
                if (!(applied > 0)) {
                  return null;
                }
                return {
                  creditEntryId: receivable.creditEntryId,
                  amount: applied,
                  outstandingBefore: receivable.outstandingBalance,
                  outstandingAfter: Math.round((receivable.outstandingBalance - applied) * 100) / 100,
                  sourceType: receivable.sourceType,
                  sourceReference: receivable.sourceReference,
                  dueDate: receivable.dueDate,
                } satisfies AllocationPreviewLine;
              })
              .filter((line): line is AllocationPreviewLine => line != null);

  const allocationErrorKey = useMemo((): MessageKey | null => {
    if (!isBusiness || amount == null || amount <= 0 || exceedsOutstanding) {
      return null;
    }
    if (allocationMode === "auto") {
      const sum = Math.round(autoPreviewLines.reduce((s, line) => s + line.amount, 0) * 100) / 100;
      if (Math.abs(sum - amount) > 1e-9) {
        return "customers.receivables.allocationSumMismatch";
      }
      return null;
    }
    if (!manualAllocationResult) {
      return null;
    }
    if (manualAllocationResult.ok) {
      return null;
    }
    switch (manualAllocationResult.error) {
      case "exceeds_receivable":
        return "customers.receivables.allocationExceeds";
      case "invalid_line":
        return "customers.receivables.allocationInvalidLine";
      case "sum_mismatch":
        return "customers.receivables.allocationSumMismatch";
      case "duplicate":
      case "unknown":
        return "customers.receivables.allocationInvalidLine";
      default:
        return "customers.receivables.allocationSumMismatch";
    }
  }, [
    isBusiness,
    amount,
    exceedsOutstanding,
    allocationMode,
    autoPreviewLines,
    manualAllocationResult,
  ]);

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

      if (props.customerKind === "personal") {
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
        await createCustomerRepayment(workspace, props.customerId, payload);
        return;
      }

      const lines =
        allocationMode === "auto"
          ? autoPreviewLines
          : manualAllocationResult?.ok
            ? manualAllocationResult.lines
            : null;
      if (!lines || lines.length === 0) {
        throw new Error(t("customers.receivables.allocationSumMismatch"));
      }
      const allocationSum = Math.round(lines.reduce((s, line) => s + line.amount, 0) * 100) / 100;
      if (Math.abs(allocationSum - amount) > 1e-9) {
        throw new Error(t("customers.receivables.allocationSumMismatch"));
      }

      await createBusinessCustomerRepayment(workspace, props.connectionId, {
        amount,
        remarks,
        paymentMethod,
        checkNumber: isCheck ? checkNumber : undefined,
        bankName: isCheck ? bankName : undefined,
        checkDate: isCheck ? checkDate : undefined,
        accountName: isCheck ? accountName : undefined,
        reference,
        allocations: lines.map((line) => ({
          creditEntryId: line.creditEntryId,
          amount: line.amount,
        })),
      });
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
        await queryClient.invalidateQueries({
          queryKey: ["business-customers", "receivables", workspace.organizationId, props.connectionId],
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

  const businessAllocationReady =
    !isBusiness ||
    (amount != null &&
      amount > 0 &&
      !exceedsOutstanding &&
      allocationErrorKey == null &&
      previewLines.length > 0);

  const canSubmit =
    !mutation.isPending &&
    props.outstandingBalance > 0 &&
    amount != null &&
    amount > 0 &&
    !exceedsOutstanding &&
    (!isCheck || Boolean(checkNumber.trim() && bankName.trim() && checkDate.trim())) &&
    businessAllocationReady;

  if (!props.open) {
    return null;
  }

  function switchToManual() {
    setAllocationMode("manual");
    setPreselectSeedActive(false);
    if (amount != null && amount > 0) {
      setManualAmounts(seedManualFromLines(openReceivables, autoPreviewLines));
    } else {
      setManualAmounts(
        Object.fromEntries(openReceivables.map((item) => [item.creditEntryId, ""])),
      );
    }
  }

  function switchToAuto() {
    setAllocationMode("auto");
    setPreselectSeedActive(false);
    setManualAmounts({});
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

          {isBusiness ? (
            <div
              className="grid gap-2 rounded-[var(--exits-radius-md)] border border-border p-3"
              data-testid="record-payment-allocation"
            >
              <div className="flex flex-wrap items-center justify-between gap-2">
                <div className="grid gap-0.5">
                  <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                    {t("customers.receivables.allocationPreview")}
                  </p>
                  <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">
                    {allocationMode === "auto"
                      ? t("customers.receivables.allocationAuto")
                      : t("customers.receivables.allocationManual")}
                  </p>
                </div>
                {allocationMode === "auto" ? (
                  <Button
                    type="button"
                    variant="outline"
                    disabled={mutation.isPending || openReceivables.length === 0}
                    onClick={switchToManual}
                    data-testid="record-payment-allocate-manually"
                  >
                    {t("customers.receivables.allocateManually")}
                  </Button>
                ) : (
                  <Button
                    type="button"
                    variant="outline"
                    disabled={mutation.isPending}
                    onClick={switchToAuto}
                    data-testid="record-payment-allocate-automatically"
                  >
                    {t("customers.receivables.allocateAutomatically")}
                  </Button>
                )}
              </div>

              {receivablesQuery.isLoading ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("customers.receivables.loading")}
                </p>
              ) : null}

              {receivablesQuery.isError ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
                  {t("customers.receivables.loadFailed")}
                </p>
              ) : null}

              {!receivablesQuery.isLoading &&
              !receivablesQuery.isError &&
              openReceivables.length === 0 ? (
                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("customers.receivables.empty")}
                </p>
              ) : null}

              {allocationMode === "manual" && openReceivables.length > 0 ? (
                <div className="grid gap-2" data-testid="record-payment-manual-allocations">
                  {openReceivables.map((receivable) => (
                    <label
                      key={receivable.creditEntryId}
                      className="grid gap-1 rounded-md border border-border/70 p-2 text-[length:var(--exits-text-sm)]"
                      data-testid={`record-payment-manual-row-${receivable.creditEntryId}`}
                    >
                      <span className="font-medium">
                        {t(receivableSourceLabelKey(receivable.sourceType))}
                        {receivable.sourceReference
                          ? ` · ${receivable.sourceReference}`
                          : ""}
                      </span>
                      <span className="text-[length:var(--exits-text-xs)] text-muted">
                        {t("customers.receivables.dueDate")}: {formatDueDate(receivable.dueDate)}
                        {" · "}
                        {t("customers.receivables.outstanding")}: ₱
                        {formatMoneyAmountInput(receivable.outstandingBalance)}
                      </span>
                      <input
                        type="text"
                        inputMode="decimal"
                        className="rounded-md border border-border bg-background px-3 tabular-nums"
                        value={manualAmounts[receivable.creditEntryId] ?? ""}
                        disabled={mutation.isPending}
                        onChange={(event) => {
                          setPreselectSeedActive(false);
                          setFormError(null);
                          const nextValue = normalizeMoneyAmountTyping(event.target.value);
                          setManualAmounts((prev) => ({
                            ...prev,
                            [receivable.creditEntryId]: nextValue,
                          }));
                        }}
                        onBlur={() => {
                          const parsed = parseMoneyAmountInput(
                            manualAmounts[receivable.creditEntryId] ?? "",
                          );
                          if (parsed !== null) {
                            setManualAmounts((prev) => ({
                              ...prev,
                              [receivable.creditEntryId]: formatMoneyAmountInput(parsed),
                            }));
                          }
                        }}
                        data-testid={`record-payment-manual-amount-${receivable.creditEntryId}`}
                      />
                    </label>
                  ))}
                </div>
              ) : null}

              {allocationMode === "auto" && previewLines.length > 0 ? (
                <ul
                  className="m-0 grid list-none gap-2 p-0"
                  data-testid="record-payment-allocation-preview"
                >
                  {previewLines.map((line) => (
                    <li
                      key={line.creditEntryId}
                      className="grid gap-0.5 rounded-md border border-border/70 p-2 text-[length:var(--exits-text-sm)]"
                      data-testid={`record-payment-allocation-line-${line.creditEntryId}`}
                    >
                      <span className="font-medium">
                        {t(receivableSourceLabelKey(line.sourceType))}
                        {line.sourceReference ? ` · ${line.sourceReference}` : ""}
                      </span>
                      <span className="text-[length:var(--exits-text-xs)] text-muted">
                        {t("customers.receivables.dueDate")}: {formatDueDate(line.dueDate)}
                      </span>
                      <span className="tabular-nums">
                        {t("customers.receivables.amountApplied")}: ₱
                        {formatMoneyAmountInput(line.amount)}
                        {" · "}
                        {t("customers.receivables.remaining")}: ₱
                        {formatMoneyAmountInput(line.outstandingAfter)}
                      </span>
                    </li>
                  ))}
                </ul>
              ) : null}

              {allocationMode === "manual" &&
              manualAllocationResult?.ok &&
              previewLines.length > 0 ? (
                <p
                  className="m-0 text-[length:var(--exits-text-xs)] text-muted"
                  data-testid="record-payment-manual-preview-summary"
                >
                  {t("customers.receivables.allocationPreviewReady")}
                </p>
              ) : null}

              {allocationErrorKey && amount != null && amount > 0 && !exceedsOutstanding ? (
                <Notice
                  tone="warning"
                  testId="record-payment-allocation-error"
                  className="px-2.5 py-1.5 text-[length:var(--exits-text-xs)]"
                >
                  {t(allocationErrorKey)}
                </Notice>
              ) : null}
            </div>
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
