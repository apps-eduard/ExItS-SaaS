import { useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import {
  getLinkedCustomerSaleReceipt,
  isExtendedHistoryRequiredError,
  type LinkedCustomerSaleReceipt,
} from "@/api/pos/pos-linked-customers-client";
import { PosApiError } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingState } from "@/components/exits/LoadingState";
import { PageHeader } from "@/components/exits/PageHeader";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { useI18n } from "@/i18n/I18nProvider";

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

  return (
    <div className={pageShell} data-testid="linked-merchant-receipt-page">
      <PageHeader
        title={receipt.receiptNumber}
        description={t("personal.merchantReceipt.lede")}
        backTo={backHref}
        backLabel={t("personal.merchantReceipt.backToStatement")}
        backTestId="page-header-back-merchant-receipt"
      />

      <section className="pc-receipt-hero exits-animate-panel" data-testid="linked-merchant-receipt-summary">
        <p className="pc-receipt-hero__number">{receipt.receiptNumber}</p>
        <p className="pc-receipt-hero__meta">
          {new Date(receipt.occurredAtUtc).toLocaleString()} · {receipt.paymentMethod} ·{" "}
          {receipt.status}
        </p>
        <p className="pc-receipt-hero__total">
          {receipt.total.toFixed(2)} {receipt.currency}
        </p>
      </section>

      <section className="pc-checkout-section exits-animate-panel">
        <p className="pc-checkout-section__title">{t("summary.disclaimerTitle")}</p>
        <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{t("summary.disclaimerBody")}</p>
      </section>

      <section className="flex flex-col gap-2 exits-animate-panel">
        <h2 className="pc-section-heading">{t("personal.merchantReceipt.linesSection")}</h2>
        <ul className="flex flex-col gap-2 m-0 p-0 list-none">
          {receipt.lines.map((line) => (
            <li key={line.lineNumber}>
              <div className="pc-receipt-line">
                <div className="min-w-0">
                  <p className="pc-receipt-line__name">{line.productNameSnapshot}</p>
                  <p className="pc-receipt-line__detail">
                    {line.quantity.toFixed(3)} {line.unitOfMeasure}
                  </p>
                </div>
                <span className="pc-receipt-line__total">{line.lineTotal.toFixed(2)}</span>
              </div>
            </li>
          ))}
        </ul>
      </section>
    </div>
  );
}
