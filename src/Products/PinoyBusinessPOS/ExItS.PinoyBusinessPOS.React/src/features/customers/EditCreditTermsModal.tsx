import { Ban, CheckCircle2, Pencil, Save } from "lucide-react";
import {
  CREDIT_TERM_PRESETS,
  creditPolicyConfigureActionLabelKey,
  creditPolicyConfigureSubmitLabelKey,
} from "@/features/customers/credit-policy";
import { ExitsModal } from "@/components/exits/ExitsModal";
import { EXITS_CANCEL_BUTTON_CLASS } from "@/components/exits/exits-cancel-button";
import { Button } from "@/components/ui/button";
import { useI18n } from "@/i18n/I18nProvider";
import {
  formatMoneyAmountInput,
  normalizeMoneyAmountTyping,
  parseMoneyAmountInput,
} from "@/lib/money-input";

export type CreditPolicyDialogMode = "configure" | "approve" | "disable";

export type EditCreditTermsModalProps = {
  open: boolean;
  mode: CreditPolicyDialogMode | null;
  subjectIdentity?: string | null;
  status: string;
  limitText: string;
  setLimitText: (value: string) => void;
  termDays: number;
  setTermDays: (value: number) => void;
  termCustom: boolean;
  setTermCustom: (value: boolean) => void;
  reason: string;
  setReason: (value: string) => void;
  showOutstandingWarning: boolean;
  showReapprovalWarning: boolean;
  busy: boolean;
  formError: string | null;
  canSubmit: boolean;
  testIdPrefix: "customer" | "business";
  onCancel: () => void;
  onSubmit: () => void;
};

export function EditCreditTermsModal({
  open,
  mode,
  subjectIdentity = null,
  status,
  limitText,
  setLimitText,
  termDays,
  setTermDays,
  termCustom,
  setTermCustom,
  reason,
  setReason,
  showOutstandingWarning,
  showReapprovalWarning,
  busy,
  formError,
  canSubmit,
  testIdPrefix,
  onCancel,
  onSubmit,
}: EditCreditTermsModalProps) {
  const { t } = useI18n();
  const dialogOpen = open && mode != null;
  const parsedLimit = parseMoneyAmountInput(limitText);

  const title =
    mode === "configure"
      ? t(creditPolicyConfigureActionLabelKey(status))
      : mode === "approve"
        ? t("customers.creditPolicy.approveCredit")
        : t("customers.creditPolicy.pauseCredit");

  return (
    <ExitsModal
      open={dialogOpen}
      onOpenChange={(next) => {
        if (!next) {
          onCancel();
        }
      }}
      title={title}
      busy={busy}
      testId={mode ? `${testIdPrefix}-credit-policy-dialog-${mode}` : `${testIdPrefix}-credit-policy-dialog`}
      closeLabel={t("customers.creditPolicy.cancel")}
      footer={
        <>
          <Button
            type="button"
            variant="outline"
            className={EXITS_CANCEL_BUTTON_CLASS}
            disabled={busy}
            onClick={onCancel}
            data-testid={`${testIdPrefix}-credit-policy-dialog-cancel`}
          >
            {t("customers.creditPolicy.cancel")}
          </Button>
          <Button
            type="button"
            disabled={!canSubmit}
            data-testid={`${testIdPrefix}-credit-policy-dialog-submit`}
            onClick={onSubmit}
          >
            {mode === "configure" ? (
              <Save className="size-4 shrink-0" aria-hidden />
            ) : mode === "approve" ? (
              <CheckCircle2 className="size-4 shrink-0" aria-hidden />
            ) : (
              <Ban className="size-4 shrink-0" aria-hidden />
            )}
            {mode === "configure"
              ? t(creditPolicyConfigureSubmitLabelKey(status))
              : t("customers.creditPolicy.save")}
          </Button>
        </>
      }
    >
      {subjectIdentity ? (
        <p
          className="m-0 mb-3 text-[length:var(--exits-text-sm)] text-muted"
          data-testid={`${testIdPrefix}-credit-policy-dialog-subject`}
        >
          {subjectIdentity}
        </p>
      ) : null}
      {mode === "configure" ? (
        <div className="grid gap-3">
          {showReapprovalWarning ? (
            <p
              className="m-0 whitespace-pre-line text-[length:var(--exits-text-sm)] text-[var(--exits-warning,var(--exits-danger))]"
              data-testid={`${testIdPrefix}-credit-policy-reapproval-warning`}
            >
              {t("customers.creditPolicy.reapprovalWarning")}
            </p>
          ) : null}
          {showOutstandingWarning ? (
            <p
              className="m-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]"
              data-testid={`${testIdPrefix}-credit-policy-outstanding-warning`}
            >
              {t("customers.creditPolicy.outstandingOverLimitWarning")}
            </p>
          ) : null}
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("customers.creditPolicy.limit")}
            <div className="exits-currency-field">
              <span className="exits-currency-field__prefix" aria-hidden>
                ₱
              </span>
              <input
                type="text"
                inputMode="decimal"
                className="exits-currency-field__input"
                value={limitText}
                onChange={(e) => setLimitText(normalizeMoneyAmountTyping(e.target.value))}
                onBlur={() => {
                  if (parsedLimit !== null) {
                    setLimitText(formatMoneyAmountInput(parsedLimit));
                  }
                }}
                data-testid={`${testIdPrefix}-credit-policy-limit-input`}
              />
            </div>
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("customers.creditPolicy.limitHelper")}
            </span>
          </label>
          <fieldset className="m-0 border-0 p-0">
            <legend className="mb-1 text-[length:var(--exits-text-sm)]">
              {t("customers.creditPolicy.term")}
            </legend>
            <div className="flex flex-wrap gap-2">
              {CREDIT_TERM_PRESETS.map((days) => (
                <Button
                  key={days}
                  type="button"
                  variant={!termCustom && termDays === days ? "default" : "outline"}
                  onClick={() => {
                    setTermCustom(false);
                    setTermDays(days);
                  }}
                  data-testid={`${testIdPrefix}-credit-policy-term-${days}`}
                >
                  {String(days)}
                </Button>
              ))}
              <Button
                type="button"
                variant={termCustom ? "default" : "outline"}
                className={
                  termCustom
                    ? undefined
                    : "border-[color-mix(in_srgb,var(--exits-info)_40%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-info)_12%,var(--exits-surface))] text-[var(--exits-info)] hover:border-[color-mix(in_srgb,var(--exits-info)_55%,var(--exits-border))] hover:bg-[color-mix(in_srgb,var(--exits-info)_18%,var(--exits-surface))]"
                }
                onClick={() => setTermCustom(true)}
                data-testid={`${testIdPrefix}-credit-policy-term-custom`}
              >
                <Pencil className="size-4 shrink-0" aria-hidden />
                {t("customers.creditPolicy.termCustom")}
              </Button>
            </div>
            <p className="mb-0 mt-1 text-[length:var(--exits-text-xs)] text-muted">
              {t("customers.creditPolicy.termExampleHelper")}
            </p>
            {termCustom ? (
              <div className="mt-2 flex flex-col gap-1">
                <span className="text-[length:var(--exits-text-sm)]">
                  {t("customers.creditPolicy.customTerm")}
                </span>
                <div className="flex items-center gap-2">
                  <input
                    type="number"
                    min={1}
                    max={365}
                    className="w-28 rounded-md border border-border bg-background px-3"
                    value={termDays}
                    onChange={(e) => setTermDays(Number(e.target.value) || 1)}
                    data-testid={`${testIdPrefix}-credit-policy-term-custom-input`}
                  />
                  <span className="text-[length:var(--exits-text-sm)] text-muted">
                    {t("customers.creditPolicy.termDaysUnit")}
                  </span>
                </div>
              </div>
            ) : null}
          </fieldset>
          <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
            {t("customers.creditPolicy.reasonForChange")}
            <textarea
              className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
              value={reason}
              maxLength={512}
              onChange={(e) => setReason(e.target.value)}
              data-testid={`${testIdPrefix}-credit-policy-reason`}
            />
            <span className="text-[length:var(--exits-text-xs)] text-muted">
              {t("customers.creditPolicy.reasonForChangeHelper")}
            </span>
          </label>
        </div>
      ) : (
        <label className="flex flex-col gap-1 text-[length:var(--exits-text-sm)]">
          {t("customers.creditPolicy.reasonForChange")}
          <textarea
            className="min-h-20 rounded-md border border-border bg-background px-3 py-2"
            value={reason}
            maxLength={512}
            onChange={(e) => setReason(e.target.value)}
            data-testid={`${testIdPrefix}-credit-policy-reason`}
          />
          <span className="text-[length:var(--exits-text-xs)] text-muted">
            {t("customers.creditPolicy.reasonForChangeHelper")}
          </span>
        </label>
      )}

      {formError ? (
        <p className="mt-3 mb-0 text-[length:var(--exits-text-sm)] text-[var(--exits-danger)]">
          {formError}
        </p>
      ) : null}
    </ExitsModal>
  );
}
