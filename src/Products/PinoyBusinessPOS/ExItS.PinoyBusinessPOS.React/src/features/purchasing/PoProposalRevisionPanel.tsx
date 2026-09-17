import { MoneyDisplay } from "@/components/exits/MoneyQuantity";
import { cn } from "@/lib/cn";
import { useI18n } from "@/i18n/I18nProvider";
import {
  formatProposalQty,
  type ProposalRevisionView,
} from "@/features/purchasing/po-proposal-revision";

export type PoProposalRevisionPanelProps = {
  revision: ProposalRevisionView;
  /** Supplier vs buyer copy for the intro lede. */
  audience: "supplier" | "buyer";
  testId?: string;
};

function formatReservedUntil(iso: string): string {
  try {
    return new Date(iso).toLocaleString();
  } catch {
    return iso;
  }
}

/**
 * Authoritative active proposal revision — requested vs proposed, totals, reservation expiry.
 */
export function PoProposalRevisionPanel({
  revision,
  audience,
  testId = "po-proposal-revision",
}: PoProposalRevisionPanelProps) {
  const { t } = useI18n();
  const diff = revision.difference;
  const diffTone = diff < 0 ? "down" : diff > 0 ? "up" : "flat";

  return (
    <section className="po-proposal-revision flex flex-col gap-3" data-testid={testId}>
      <div
        className="incoming-order-stock-summary incoming-order-stock-summary--adjustment"
        data-testid={`${testId}-banner`}
      >
        <p className="incoming-order-stock-summary__title m-0">
          {t("incomingOrders.changesProposedTitle")}
        </p>
        <p className="incoming-order-stock-summary__detail m-0">
          {t("incomingOrders.awaitingBuyerReview")}
        </p>
        {revision.reservationExpiresAtUtc ? (
          <p
            className="incoming-order-stock-summary__hint m-0"
            data-testid={`${testId}-reserved-until`}
          >
            {t("purchasing.reservedUntil").replace(
              "{datetime}",
              formatReservedUntil(revision.reservationExpiresAtUtc),
            )}
          </p>
        ) : null}
      </div>

      <div
        className="incoming-order-stock-summary"
        data-testid={`${testId}-summary`}
      >
        <p className="incoming-order-stock-summary__title m-0">
          {t("incomingOrders.supplierProposedChanges")}
        </p>
        <p className="incoming-order-stock-summary__detail m-0">
          {audience === "buyer"
            ? t("incomingOrders.buyerMustReviewProposal")
            : t("incomingOrders.buyerMustReviewProposal")}
        </p>
      </div>

      <div className="po-document-lines__table-wrap" data-testid={`${testId}-table`}>
        <table className="po-document-lines__table">
          <thead>
            <tr>
              <th scope="col">{t("purchasing.colProduct")}</th>
              <th scope="col" className="po-document-lines__num">
                {t("incomingOrders.colRequestedQty")}
              </th>
              <th scope="col" className="po-document-lines__num">
                {t("incomingOrders.colProposedQty")}
              </th>
              <th scope="col" className="po-document-lines__num">
                {t("purchasing.unitCost")}
              </th>
              <th scope="col" className="po-document-lines__num">
                {t("incomingOrders.colProposedLineTotal")}
              </th>
            </tr>
          </thead>
          <tbody>
            {revision.lines.map((line) => (
              <tr
                key={line.productId}
                data-testid={`${testId}-line-${line.productId}`}
                data-changed={line.changed ? "true" : "false"}
              >
                <td>
                  <div className="font-medium">{line.productName}</div>
                  {line.sku ? (
                    <div className="text-[length:var(--exits-text-xs)] text-muted">{line.sku}</div>
                  ) : null}
                </td>
                <td className="po-document-lines__num tabular-nums">
                  {formatProposalQty(line.requestedQty, line.unitOfMeasureCode)}
                </td>
                <td
                  className={cn(
                    "po-document-lines__num tabular-nums",
                    line.changed && "incoming-order-stock-qty--warning font-semibold",
                  )}
                  data-testid={`${testId}-proposed-qty-${line.productId}`}
                >
                  {line.unavailable
                    ? t("incomingOrders.unavailable")
                    : formatProposalQty(line.proposedQty, line.unitOfMeasureCode)}
                </td>
                <td
                  className={cn(
                    "po-document-lines__num",
                    line.priceChanged && "incoming-order-stock-qty--warning font-semibold",
                  )}
                >
                  <MoneyDisplay amount={line.proposedUnitCost} />
                </td>
                <td
                  className="po-document-lines__num font-semibold"
                  data-testid={`${testId}-line-total-${line.productId}`}
                >
                  <MoneyDisplay amount={line.proposedLineTotal} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <ul
        className="po-document-lines__mobile m-0 list-none flex-col gap-2 p-0"
        data-testid={`${testId}-mobile`}
      >
        {revision.lines.map((line) => (
          <li
            key={line.productId}
            className={cn(
              "po-document-lines__mobile-row",
              line.changed && "incoming-order-stock-line--shortage",
            )}
            data-testid={`${testId}-line-mobile-${line.productId}`}
          >
            <div className="po-document-lines__mobile-title-row">
              <p className="m-0 font-medium">{line.productName}</p>
              <p className="m-0 tabular-nums font-semibold">
                <MoneyDisplay amount={line.proposedLineTotal} />
              </p>
            </div>
            {line.sku ? (
              <p className="m-0 text-[length:var(--exits-text-xs)] text-muted">{line.sku}</p>
            ) : null}
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
              {t("incomingOrders.colRequestedQty")}:{" "}
              {formatProposalQty(line.requestedQty, line.unitOfMeasureCode)}
            </p>
            <p
              className={cn(
                "m-0 text-[length:var(--exits-text-sm)]",
                line.changed ? "incoming-order-stock-qty--warning font-semibold" : "text-muted",
              )}
            >
              {t("incomingOrders.colProposedQty")}:{" "}
              {line.unavailable
                ? t("incomingOrders.unavailable")
                : formatProposalQty(line.proposedQty, line.unitOfMeasureCode)}
            </p>
            <p className="m-0 text-[length:var(--exits-text-sm)] text-muted tabular-nums">
              {t("purchasing.unitCost")}: <MoneyDisplay amount={line.proposedUnitCost} />
            </p>
          </li>
        ))}
      </ul>

      <div
        className="po-proposal-totals flex flex-col gap-1.5 ms-auto w-full max-w-[20rem]"
        data-testid={`${testId}-totals`}
      >
        <div className="po-document-totals__row">
          <span className="po-document-totals__label">{t("incomingOrders.originalOrderTotal")}</span>
          <span className="po-document-totals__value" data-testid={`${testId}-original-total`}>
            <MoneyDisplay amount={revision.originalTotal} />
          </span>
        </div>
        <div className="po-document-totals__row po-document-totals__row--strong">
          <span className="po-document-totals__label">{t("incomingOrders.proposedOrderTotal")}</span>
          <span className="po-document-totals__value" data-testid={`${testId}-proposed-total`}>
            <MoneyDisplay amount={revision.proposedTotal} />
          </span>
        </div>
        <div
          className={cn(
            "po-document-totals__row",
            diffTone === "down" && "po-proposal-diff--down",
            diffTone === "up" && "po-proposal-diff--up",
          )}
          data-testid={`${testId}-difference`}
        >
          <span className="po-document-totals__label">{t("incomingOrders.proposalDifference")}</span>
          <span className="po-document-totals__value tabular-nums font-medium">
            {diff === 0 ? (
              <MoneyDisplay amount={0} />
            ) : (
              <>
                {diff < 0 ? "−" : "+"}
                <MoneyDisplay amount={Math.abs(diff)} />
              </>
            )}
          </span>
        </div>
      </div>
    </section>
  );
}
