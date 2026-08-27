import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { ChevronDown } from "lucide-react";
import {
  getLinkedCustomerSaleReceipt,
  isExtendedHistoryRequiredError,
  type LinkedCustomerSaleReceipt,
} from "@/api/pos/pos-linked-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import {
  personalStoreDisplayName,
  stripPersonalRunStamp,
} from "@/features/customer-ordering/format-personal-store-label";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";

type ReceiptState =
  | { kind: "loading" }
  | { kind: "offline" }
  | { kind: "forbidden"; detail: string }
  | { kind: "notFound"; detail: string }
  | { kind: "entitlement"; detail: string }
  | { kind: "error"; detail: string }
  | { kind: "ready"; receipt: LinkedCustomerSaleReceipt };

export function LinkedMerchantReceiptPage() {
  const { t } = useI18n();
  const online = useBrowserOnline();
  const {
    organizationId = "",
    businessCustomerId = "",
    saleId = "",
  } = useParams<{
    organizationId: string;
    businessCustomerId: string;
    saleId: string;
  }>();
  const [state, setState] = useState<ReceiptState>({ kind: "loading" });
  const [disclaimerOpen, setDisclaimerOpen] = useState(true);

  const backHref = `/personal/linked-merchants/${organizationId}/${businessCustomerId}`;
  const pageShell =
    "personal-page personal-commerce-page linked-merchant-receipt-page exits-page flex min-w-0 flex-col gap-3";

  useEffect(() => {
    async function load() {
      if (!organizationId || !businessCustomerId || !saleId) {
        setState({ kind: "notFound", detail: t("personal.merchantReceipt.missing") });
        return;
      }
      if (!online) {
        setState({ kind: "offline" });
        return;
      }

      setState({ kind: "loading" });
      try {
        const receipt = await getLinkedCustomerSaleReceipt(
          organizationId,
          businessCustomerId,
          saleId,
        );
        setState({ kind: "ready", receipt });
      } catch (err) {
        if (isExtendedHistoryRequiredError(err)) {
          setState({
            kind: "entitlement",
            detail: t("personal.merchantStatement.historyLocked"),
          });
          return;
        }
        if (err instanceof PosApiError) {
          if (err.status === 403) {
            setState({ kind: "forbidden", detail: err.message });
            return;
          }
          if (err.status === 404) {
            setState({ kind: "notFound", detail: err.message });
            return;
          }
        }
        setState({
          kind: "error",
          detail: err instanceof Error ? err.message : t("personal.merchantReceipt.loadFailed"),
        });
      }
    }

    void load();
  }, [businessCustomerId, online, organizationId, saleId, t]);

  if (state.kind === "loading") {
    return <LoadingState label={t("loading.label")} />;
  }

  if (state.kind === "offline") {
    return (
      <ErrorState
        title={t("offline.internetRequiredTitle")}
        detail={t("offline.requiredHistory")}
      />
    );
  }

  if (state.kind === "forbidden") {
    return (
      <ErrorState
        title={t("personal.merchantStatement.deniedTitle")}
        detail={state.detail || t("personal.merchantStatement.denied")}
      />
    );
  }

  if (state.kind === "notFound") {
    return (
      <ErrorState
        title={t("personal.merchantReceipt.missingTitle")}
        detail={state.detail || t("personal.merchantReceipt.missing")}
      />
    );
  }

  if (state.kind === "entitlement") {
    return (
      <div className={pageShell} data-testid="linked-merchant-receipt-page">
        <PageHeader
          title={t("personal.merchantReceipt.missingTitle")}
          backTo={backHref}
          backLabel={t("personal.merchantReceipt.backToStatement")}
          backTestId="page-header-back-merchant-receipt"
        />
        <ErrorState
          title={t("personal.merchantStatement.historyLockedTitle")}
          detail={state.detail}
        />
        <Button asChild className="min-h-11 w-fit">
          <Link to="/personal/rewards">{t("personal.merchantStatement.historyUnlock")}</Link>
        </Button>
      </div>
    );
  }

  if (state.kind === "error") {
    return (
      <div className={pageShell} data-testid="linked-merchant-receipt-page">
        <PageHeader
          title={t("personal.merchantReceipt.errorTitle")}
          backTo={backHref}
          backLabel={t("personal.merchantReceipt.backToStatement")}
          backTestId="page-header-back-merchant-receipt"
        />
        <ErrorState title={t("personal.merchantReceipt.errorTitle")} detail={state.detail} />
      </div>
    );
  }

  const { receipt } = state;
  const subtotal = receipt.subtotal;

  return (
    <div className={pageShell} data-testid="linked-merchant-receipt-page">
      <PageHeader
        title={receipt.receiptNumber}
        description={t("personal.merchantReceipt.lede")}
        backTo={backHref}
        backLabel={t("personal.merchantReceipt.backToStatement")}
        backTestId="page-header-back-merchant-receipt"
      />

      <section
        className="pc-receipt-disclaimer exits-animate-panel"
        data-testid="linked-merchant-receipt-disclaimer"
      >
        <button
          type="button"
          className="pc-receipt-disclaimer__toggle"
          aria-expanded={disclaimerOpen}
          data-testid="linked-merchant-receipt-disclaimer-toggle"
          onClick={() => setDisclaimerOpen((open) => !open)}
        >
          <span>{t("summary.disclaimerTitle")}</span>
          <ChevronDown
            className={cn(
              "pc-receipt-disclaimer__chevron size-4 shrink-0",
              disclaimerOpen && "pc-receipt-disclaimer__chevron--open",
            )}
            aria-hidden
          />
        </button>
        {disclaimerOpen ? (
          <p
            className="pc-receipt-disclaimer__body"
            data-testid="linked-merchant-receipt-disclaimer-body"
          >
            {t("summary.disclaimerBody")}
          </p>
        ) : null}
      </section>

      <section
        className="pc-receipt-card exits-animate-panel"
        data-testid="linked-merchant-receipt-summary"
      >
        <dl className="pc-receipt-card__meta-list m-0">
          <div className="pc-receipt-card__meta-row">
            <dt>{t("summary.saleNumber")}</dt>
            <dd className="font-semibold" data-testid="linked-merchant-receipt-number">
              {receipt.receiptNumber}
            </dd>
          </div>
          <div className="pc-receipt-card__meta-row">
            <dt>{t("summary.dateTime")}</dt>
            <dd data-testid="linked-merchant-receipt-datetime">
              {new Date(receipt.occurredAtUtc).toLocaleString()}
            </dd>
          </div>
          <div className="pc-receipt-card__meta-row">
            <dt>{t("summary.paymentMethod")}</dt>
            <dd data-testid="linked-merchant-receipt-payment">{receipt.paymentMethod}</dd>
          </div>
          <div className="pc-receipt-card__meta-row">
            <dt>{t("summary.status")}</dt>
            <dd data-testid="linked-merchant-receipt-status">{receipt.status}</dd>
          </div>
          {personalStoreDisplayName(receipt.merchantDisplayName) ? (
            <div className="pc-receipt-card__meta-row">
              <dt>{t("personal.merchantReceipt.store")}</dt>
              <dd data-testid="linked-merchant-receipt-store">
                {personalStoreDisplayName(receipt.merchantDisplayName)}
              </dd>
            </div>
          ) : null}
          {stripPersonalRunStamp(receipt.customerDisplayName ?? "") ? (
            <div className="pc-receipt-card__meta-row">
              <dt>{t("summary.customer")}</dt>
              <dd data-testid="linked-merchant-receipt-customer">
                {stripPersonalRunStamp(receipt.customerDisplayName ?? "")}
              </dd>
            </div>
          ) : null}
        </dl>

        <ul className="pc-receipt-card__line-list m-0 list-none p-0">
          {receipt.lines.map((line) => (
            <li
              key={line.lineNumber}
              className="pc-receipt-card__line"
              data-testid={`linked-merchant-receipt-line-${line.lineNumber}`}
            >
              <span className="min-w-0 truncate text-[length:var(--exits-text-sm)]">
                {line.productNameSnapshot} × {line.quantity} {line.unitOfMeasure}
              </span>
              <MoneyDisplay amount={line.lineTotal} className="pc-receipt-line__total" />
            </li>
          ))}
        </ul>

        <div className="pc-receipt-card__totals">
          <p className="pc-receipt-card__total-row">
            <span className="text-muted">{t("summary.subtotal")}</span>
            <MoneyDisplay amount={subtotal} />
          </p>
          <p className="pc-receipt-card__total-row pc-receipt-card__total-row--emphasis">
            <span>{t("summary.total")}</span>
            <MoneyDisplay amount={receipt.total} testId="linked-merchant-receipt-total" />
          </p>
        </div>
      </section>
    </div>
  );
}
