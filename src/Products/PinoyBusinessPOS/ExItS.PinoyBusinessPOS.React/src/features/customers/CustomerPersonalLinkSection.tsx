import type { ReactNode } from "react";
import { Clock, UserRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ConnectionStatusChip } from "@/features/customer-connection/ConnectionStatusChip";
import { mapOrgLinkStatusToRelationship } from "@/features/customer-connection/connection-state";
import type { CustomerLinkUiStatus } from "@/features/customers/customer-link-status";
import type { CustomerLinkStatusDto } from "@/api/platform/customer-link-status-client";
import { useI18n } from "@/i18n/I18nProvider";

type LinkHistoryItem = {
  id: string;
  status: string;
  createdAtUtc: string;
};

export type CustomerPersonalLinkSectionProps = {
  linkUiStatus: CustomerLinkUiStatus;
  personalExItsId: string | null;
  customerDisplayName: string;
  linkMeta: CustomerLinkStatusDto | undefined;
  linkHistoryItems: LinkHistoryItem[];
  /** Optional peer card (e.g. Delivery) shown beside Connection history. */
  historyPeer?: ReactNode;
  showAfterCreateHint: boolean;
  afterCreateHintDismissed: boolean;
  onDismissAfterCreateHint: () => void;
  online: boolean;
  allowEdit: boolean;
  reminderCooldownActive: boolean;
  remindPending: boolean;
  revokePending: boolean;
  onRemind: () => void;
  onRevoke: () => void;
};

export function CustomerPersonalLinkSection({
  linkUiStatus,
  personalExItsId,
  customerDisplayName,
  linkMeta,
  linkHistoryItems,
  historyPeer,
  showAfterCreateHint,
  afterCreateHintDismissed,
  onDismissAfterCreateHint,
  online,
  allowEdit,
  reminderCooldownActive,
  remindPending,
  revokePending,
  onRemind,
  onRevoke,
}: CustomerPersonalLinkSectionProps) {
  const { t } = useI18n();
  const relationship = mapOrgLinkStatusToRelationship(linkUiStatus);
  const isPending = linkUiStatus === "Pending";
  const showPendingCard = isPending || (showAfterCreateHint && !afterCreateHintDismissed);

  if (linkUiStatus === "NotLinked") {
    return null;
  }

  return (
    <>
      {showAfterCreateHint && !afterCreateHintDismissed && isPending ? (
        <Card
          data-testid="customer-link-after-create-success"
          className="border-[color-mix(in_srgb,var(--exits-success)_35%,var(--exits-border))] bg-[color-mix(in_srgb,var(--exits-success)_6%,transparent)]"
        >
          <div className="flex flex-wrap items-start justify-between gap-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("customers.linkAfterCreateSuccess")}
            </p>
            <Button
              type="button"
              variant="ghost"
              className="min-h-8 shrink-0 px-2 text-[length:var(--exits-text-sm)]"
              data-testid="customer-link-dismiss-after-create"
              onClick={onDismissAfterCreateHint}
            >
              {t("customers.linkPendingDismissSuccess")}
            </Button>
          </div>
          <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.linkPendingAfterCreate")}
          </p>
        </Card>
      ) : null}

      {isPending ? (
        <Card
          data-testid="customer-link-pending-banner"
          className="border-[color-mix(in_srgb,var(--exits-info)_35%,var(--exits-border))]"
        >
          <div className="flex flex-wrap items-start gap-3">
            <span
              className="flex size-10 shrink-0 items-center justify-center rounded-full bg-[color-mix(in_srgb,var(--exits-info)_14%,transparent)] text-[var(--exits-info)]"
              aria-hidden
            >
              <Clock className="size-5" />
            </span>
            <div className="min-w-0 flex-1">
              <div className="flex flex-wrap items-center gap-2">
                <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                  {t("customers.linkPendingTitle")}
                </p>
                <ConnectionStatusChip
                  state={relationship}
                  audience="organization"
                  testId="customer-connection-status-chip-inline"
                />
              </div>
              <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
                {t("customers.linkPendingBanner")}
              </p>
            </div>
          </div>

          <p className="mb-0 mt-2 text-[length:var(--exits-text-sm)] text-muted">
            {t("customers.linkPendingExItsHint")}
          </p>

          {linkMeta?.invitationSentAtUtc || (linkMeta?.reminderCount ?? 0) > 0 ? (
            <dl className="mb-0 mt-3 grid gap-2 text-[length:var(--exits-text-sm)] sm:grid-cols-2">
              {linkMeta?.invitationSentAtUtc ? (
                <div data-testid="customer-link-invitation-sent">
                  <dt className="text-muted">{t("customers.linkInvitationSentLabel")}</dt>
                  <dd className="m-0 font-medium">
                    {new Date(linkMeta.invitationSentAtUtc).toLocaleString()}
                  </dd>
                </div>
              ) : null}
              {(linkMeta?.reminderCount ?? 0) > 0 && linkMeta?.lastRemindedAtUtc ? (
                <div data-testid="customer-link-last-reminder">
                  <dt className="text-muted">{t("customers.linkLastReminder")}</dt>
                  <dd className="m-0 font-medium">
                    {new Date(linkMeta.lastRemindedAtUtc).toLocaleString()}
                    {" · "}
                    {t("customers.linkRemindersCount").replace(
                      "{count}",
                      String(linkMeta.reminderCount),
                    )}
                  </dd>
                </div>
              ) : null}
            </dl>
          ) : null}

          <div className="mt-4 rounded-md border border-dashed border-[var(--exits-border)] px-3 py-3">
            <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
              {t("customers.linkPendingNextStepsTitle")}
            </p>
            <ol className="mb-0 mt-2 list-decimal space-y-1.5 pl-5 text-[length:var(--exits-text-sm)] text-muted">
              <li>{t("customers.linkPendingStep1").replace("{name}", customerDisplayName)}</li>
              <li>{t("customers.linkPendingStep2")}</li>
              <li>{t("customers.linkPendingStep3")}</li>
            </ol>
          </div>

          {showPendingCard && online && allowEdit && linkMeta?.latestLinkRequestId ? (
            <div className="mt-4 flex flex-wrap gap-2">
              <Button
                type="button"
                data-testid="customer-link-remind"
                disabled={reminderCooldownActive || remindPending}
                onClick={onRemind}
              >
                {t("customers.linkRemind")}
              </Button>
              <Button
                type="button"
                variant="outline"
                data-testid="customer-link-cancel-invitation"
                disabled={revokePending}
                onClick={onRevoke}
              >
                {t("customers.linkCancelInvitation")}
              </Button>
              {reminderCooldownActive ? (
                <p className="m-0 w-full text-[length:var(--exits-text-sm)] text-muted">
                  {t("customers.linkRemindCooldown")}
                </p>
              ) : null}
            </div>
          ) : null}
        </Card>
      ) : null}

      {personalExItsId || historyPeer || linkHistoryItems.length > 0 ? (
        <div className="customer-link-peer-grid">
          {personalExItsId ? (
            <Card className="flex flex-col gap-3 p-4" data-testid="customer-link-exits-id-panel">
              <div className="flex items-start gap-2">
                <span
                  className="flex size-9 shrink-0 items-center justify-center rounded-full bg-[color-mix(in_srgb,var(--exits-border)_60%,transparent)] text-muted"
                  aria-hidden
                >
                  <UserRound className="size-4" />
                </span>
                <div className="min-w-0 flex flex-col gap-1">
                  <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                    {t("customers.exItsIdLabel")}
                  </p>
                  <p
                    className="mb-0 font-mono text-[length:var(--exits-text-md)] font-semibold tracking-wide"
                    data-testid="customer-exits-id"
                  >
                    {personalExItsId}
                  </p>
                </div>
              </div>
            </Card>
          ) : null}

          {historyPeer}

          {linkHistoryItems.length > 0 ? (
            <Card className="flex min-w-0 flex-col gap-3 p-4" data-testid="customer-link-history">
              <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
                {t("customers.linkHistoryTitle")}
              </p>
              <ul className="mb-0 list-none space-y-1.5 p-0">
                {linkHistoryItems.map((item) => {
                  const historyState = mapOrgLinkStatusToRelationship(item.status);
                  return (
                    <li
                      key={item.id}
                      className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1 text-[length:var(--exits-text-sm)]"
                    >
                      <span className="text-muted">
                        {new Date(item.createdAtUtc).toLocaleString()}
                      </span>
                      <ConnectionStatusChip
                        state={historyState}
                        audience="organization"
                        testId={`customer-link-history-status-${item.id}`}
                      />
                    </li>
                  );
                })}
              </ul>
            </Card>
          ) : null}
        </div>
      ) : null}
    </>
  );
}
