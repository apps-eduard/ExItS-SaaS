import { Link } from "react-router-dom";
import {
  AlertCircle,
  ArrowLeft,
  FileQuestion,
  Link2,
  Receipt,
  RefreshCw,
  ShieldX,
  ShoppingBag,
  WifiOff,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { ConnectionStatusChip } from "@/features/customer-connection/ConnectionStatusChip";
import { storeDisplayInitial } from "@/features/customer-ordering/personal-commerce-ui";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import { personalPageBackNav } from "@/navigation/page-back-nav";

export type MerchantStatementStatusVariant =
  | "notFound"
  | "historyNotReady"
  | "historyLoadError"
  | "forbidden"
  | "error"
  | "offline"
  | "empty";

type MerchantStatementStatusPanelProps = {
  variant: MerchantStatementStatusVariant;
  storeName?: string;
  relationshipLabel?: string | null;
  /** When true, show Connected chip — relationship is Linked; panel is only about data load. */
  showConnectedRelationship?: boolean;
  detail?: string | null;
  onRetry?: () => void;
  shopTo?: string | null;
};

function variantMeta(variant: MerchantStatementStatusVariant): {
  titleKey: MessageKey;
  detailKey: MessageKey;
  hintKey?: MessageKey;
  Icon: typeof Receipt;
  tone: "neutral" | "warning" | "danger" | "info";
} {
  switch (variant) {
    case "notFound":
      return {
        titleKey: "personal.merchantStatement.missingTitle",
        detailKey: "personal.merchantStatement.missing",
        hintKey: "personal.merchantStatement.missingHint",
        Icon: FileQuestion,
        tone: "warning",
      };
    case "historyNotReady":
      return {
        titleKey: "personal.merchantStatement.historyNotReadyTitle",
        detailKey: "personal.merchantStatement.historyNotReady",
        hintKey: "personal.merchantStatement.historyNotReadyHint",
        Icon: FileQuestion,
        tone: "info",
      };
    case "historyLoadError":
      return {
        titleKey: "personal.merchantStatement.historyLoadErrorTitle",
        detailKey: "personal.merchantStatement.historyLoadError",
        hintKey: "personal.merchantStatement.historyLoadErrorHint",
        Icon: AlertCircle,
        tone: "warning",
      };
    case "forbidden":
      return {
        titleKey: "personal.merchantStatement.deniedTitle",
        detailKey: "personal.merchantStatement.denied",
        hintKey: "personal.merchantStatement.deniedHint",
        Icon: ShieldX,
        tone: "danger",
      };
    case "error":
      return {
        titleKey: "personal.merchantStatement.errorTitle",
        detailKey: "personal.merchantStatement.loadFailed",
        Icon: AlertCircle,
        tone: "danger",
      };
    case "offline":
      return {
        titleKey: "offline.internetRequiredTitle",
        detailKey: "offline.requiredHistory",
        Icon: WifiOff,
        tone: "warning",
      };
    case "empty":
      return {
        titleKey: "personal.merchantStatement.noActivityTitle",
        detailKey: "personal.merchantStatement.noActivityDetail",
        hintKey: "personal.merchantStatement.noActivityHint",
        Icon: Receipt,
        tone: "neutral",
      };
  }
}

export function MerchantStatementStatusPanel({
  variant,
  storeName,
  relationshipLabel,
  showConnectedRelationship = false,
  detail,
  onRetry,
  shopTo,
}: MerchantStatementStatusPanelProps) {
  const { t } = useI18n();
  const meta = variantMeta(variant);
  const displayName = storeName?.trim() || t("personal.merchantStatement.title");
  const resolvedDetail = detail?.trim() || t(meta.detailKey);
  const actionCount = 1 + (onRetry ? 1 : 0) + (shopTo ? 1 : 0);

  return (
    <section
      className={cn(
        "pc-statement-status exits-animate-panel",
        `pc-statement-status--${meta.tone}`,
      )}
      data-testid={`merchant-statement-status-${variant}`}
      aria-labelledby="merchant-statement-status-title"
    >
      <div className="pc-statement-status__hero">
        <span className="pc-statement-status__avatar" aria-hidden>
          {storeDisplayInitial(displayName)}
        </span>
        <div className="pc-statement-status__identity min-w-0 flex-1">
          <h2 id="merchant-statement-status-title" className="pc-statement-status__store">
            {displayName}
          </h2>
          {showConnectedRelationship ? (
            <div className="mt-1.5 flex flex-wrap items-center gap-2">
              <ConnectionStatusChip
                state="Linked"
                audience="personal"
                testId="merchant-statement-connection-chip"
              />
              {relationshipLabel ? (
                <span className="pc-statement-status__relationship text-[length:var(--exits-text-sm)] text-muted">
                  {relationshipLabel}
                </span>
              ) : null}
            </div>
          ) : relationshipLabel ? (
            <>
              <Link2 className="pc-statement-status__link-icon size-3.5 shrink-0" aria-hidden />
              <span className="pc-statement-status__relationship">{relationshipLabel}</span>
            </>
          ) : null}
        </div>
      </div>

      {showConnectedRelationship ? (
        <p className="pc-statement-status__connected-copy m-0 text-[length:var(--exits-text-sm)] text-muted">
          {t("connection.detail.personal.connected")}
        </p>
      ) : null}

      <div className="pc-statement-status__message">
        <span className="pc-statement-status__icon-wrap" aria-hidden>
          <meta.Icon className="pc-statement-status__icon" />
        </span>
        <div className="min-w-0">
          <p className="pc-statement-status__headline">{t(meta.titleKey)}</p>
          <p className="pc-statement-status__detail">{resolvedDetail}</p>
          {meta.hintKey ? (
            <p className="pc-statement-status__hint">{t(meta.hintKey)}</p>
          ) : null}
        </div>
      </div>

      <div
        className={cn(
          "pc-statement-status__actions",
          actionCount === 1 && "pc-statement-status__actions--solo",
          actionCount === 2 && "pc-statement-status__actions--duo",
          actionCount >= 3 && "pc-statement-status__actions--triple",
        )}
      >
        <Button asChild className="pc-statement-status__action">
          <Link
            to={personalPageBackNav.merchants.to}
            data-testid="merchant-statement-back-stores"
          >
            <ArrowLeft className="size-4 shrink-0" aria-hidden />
            {t("personal.merchantStatement.backToStores")}
          </Link>
        </Button>
        {onRetry ? (
          <Button
            type="button"
            variant="outline"
            className="pc-statement-status__action pc-statement-status__action--accent gap-2"
            data-testid="merchant-statement-retry"
            onClick={onRetry}
          >
            <RefreshCw className="size-4 shrink-0" aria-hidden />
            {t("orders.retry")}
          </Button>
        ) : null}
        {shopTo ? (
          <Button asChild variant="outline" className="pc-statement-status__action pc-statement-status__action--accent">
            <Link to={shopTo} data-testid="merchant-statement-open-shop">
              <ShoppingBag className="size-4 shrink-0" aria-hidden />
              {t("personal.shopLink")}
            </Link>
          </Button>
        ) : null}
      </div>
    </section>
  );
}
