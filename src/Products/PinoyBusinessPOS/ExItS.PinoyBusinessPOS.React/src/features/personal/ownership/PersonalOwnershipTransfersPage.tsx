import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { Building2, CalendarClock, Check, Loader2, X } from "lucide-react";
import {
  acceptOwnershipTransfer,
  declineOwnershipTransfer,
  listMyPendingOwnershipTransfers,
  type OrganizationOwnershipTransferDto,
} from "@/api/platform/ownership-transfer-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/exits/EmptyState";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { formatShortDate } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { personalPageBackNav } from "@/navigation/page-back-nav";
import { ONLINE_REQUIRED_CODES, onlineRequiredDetailKey } from "@/offline/online-required";
import { useSwitchToBusiness } from "@/workspace/use-switch-to-business";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export const PERSONAL_OWNERSHIP_TRANSFERS_QUERY_KEY = [
  "personal",
  "ownership-transfers",
  "pending",
] as const;

function organizationLabel(transfer: OrganizationOwnershipTransferDto): string {
  return (
    transfer.organizationDisplayName?.trim() ||
    transfer.publicOrganizationId?.trim() ||
    transfer.organizationId
  );
}

function isTransferExpired(transfer: OrganizationOwnershipTransferDto, nowMs: number): boolean {
  if (transfer.status.trim().toLowerCase() === "expired") {
    return true;
  }
  const expires = Date.parse(transfer.expiresAtUtc);
  return Number.isFinite(expires) && expires < nowMs;
}

function isConflictError(error: unknown): boolean {
  if (!(error instanceof PlatformApiError)) {
    return false;
  }
  if (error.status === 409) {
    return true;
  }
  const code = (error.errorCode ?? "").toLowerCase();
  return code.includes("ownership_transfer") && code.includes("conflict");
}

function OfflineNotice({ message }: { message: string }) {
  return (
    <p
      className="m-0 text-[length:var(--exits-text-sm)] text-muted"
      data-testid="ownership-transfer-offline"
    >
      {message}
    </p>
  );
}

type ConfirmMode = "accept" | "decline" | null;

type SuccessState = {
  organizationName: string;
};

export function PersonalOwnershipTransfersPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const { refreshWorkspaces } = useWorkspace();
  const { switching, switchToBusiness } = useSwitchToBusiness();
  const [confirmForId, setConfirmForId] = useState<string | null>(null);
  const [confirmMode, setConfirmMode] = useState<ConfirmMode>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [success, setSuccess] = useState<SuccessState | null>(null);

  const pendingQuery = useQuery({
    queryKey: PERSONAL_OWNERSHIP_TRANSFERS_QUERY_KEY,
    queryFn: ({ signal }) => listMyPendingOwnershipTransfers(signal),
  });

  const acceptMutation = useMutation({
    mutationFn: (transferId: string) => acceptOwnershipTransfer(transferId),
  });

  const declineMutation = useMutation({
    mutationFn: (transferId: string) => declineOwnershipTransfer(transferId),
  });

  const busy = acceptMutation.isPending || declineMutation.isPending;
  const offlineBlocked = !online;
  const nowMs = Date.now();

  async function refetchPending() {
    await queryClient.invalidateQueries({ queryKey: PERSONAL_OWNERSHIP_TRANSFERS_QUERY_KEY });
  }

  function openConfirm(transferId: string, mode: ConfirmMode) {
    if (busy || offlineBlocked) return;
    setActionError(null);
    setConfirmForId(transferId);
    setConfirmMode(mode);
  }

  function closeConfirm() {
    if (busy) return;
    setConfirmForId(null);
    setConfirmMode(null);
  }

  async function onAcceptConfirm(transfer: OrganizationOwnershipTransferDto) {
    if (offlineBlocked) {
      setActionError(t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalOwnershipTransfer)));
      return;
    }
    setActionError(null);
    try {
      await acceptMutation.mutateAsync(transfer.id);
      setSuccess({ organizationName: organizationLabel(transfer) });
      setConfirmForId(null);
      setConfirmMode(null);
    } catch (error) {
      if (isConflictError(error)) {
        await refetchPending();
        setActionError(t("personal.ownershipTransfers.staleConflict"));
        setConfirmForId(null);
        setConfirmMode(null);
        return;
      }
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.ownershipTransfers.acceptFailed"),
      );
    }
  }

  async function onDeclineConfirm(transfer: OrganizationOwnershipTransferDto) {
    if (offlineBlocked) {
      setActionError(t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalOwnershipTransfer)));
      return;
    }
    setActionError(null);
    try {
      await declineMutation.mutateAsync(transfer.id);
      await refetchPending();
      setConfirmForId(null);
      setConfirmMode(null);
    } catch (error) {
      if (isConflictError(error)) {
        await refetchPending();
        setActionError(t("personal.ownershipTransfers.staleConflict"));
        setConfirmForId(null);
        setConfirmMode(null);
        return;
      }
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("personal.ownershipTransfers.declineFailed"),
      );
    }
  }

  if (success) {
    return (
      <div
        className="personal-page exits-page flex min-w-0 flex-col gap-3"
        data-testid="personal-ownership-transfers-page"
      >
        <PageHeader
          title={t("personal.ownershipTransfers.title")}
          description={t("personal.ownershipTransfers.lede")}
          backTo={personalPageBackNav.more.to}
          backLabel={t(personalPageBackNav.more.labelKey)}
          backTestId="page-header-back-ownership-transfers"
        />
        <section
          className="catalog-form-section exits-animate-panel personal-section gap-3"
          data-testid="ownership-transfer-success"
        >
          <h2 className="catalog-form-section__title m-0">
            {t("personal.ownershipTransfers.successTitle").replace(
              "{name}",
              success.organizationName,
            )}
          </h2>
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("personal.ownershipTransfers.successDetail")}
          </p>
          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              disabled={switching || offlineBlocked}
              data-testid="ownership-go-to-business"
              onClick={() => {
                void (async () => {
                  await refreshWorkspaces();
                  await switchToBusiness();
                })();
              }}
            >
              {switching
                ? t("personal.more.switchingBusiness")
                : t("personal.ownershipTransfers.goToBusiness")}
            </Button>
            <Button
              type="button"
              variant="outline"
              data-testid="ownership-stay-personal"
              onClick={() => {
                setSuccess(null);
                void queryClient.invalidateQueries({
                  queryKey: PERSONAL_OWNERSHIP_TRANSFERS_QUERY_KEY,
                });
                void refreshWorkspaces();
              }}
            >
              {t("personal.ownershipTransfers.stayPersonal")}
            </Button>
          </div>
        </section>
      </div>
    );
  }

  return (
    <div
      className="personal-page exits-page flex min-w-0 flex-col gap-3"
      data-testid="personal-ownership-transfers-page"
    >
      <PageHeader
        title={t("personal.ownershipTransfers.title")}
        description={t("personal.ownershipTransfers.lede")}
        backTo={personalPageBackNav.more.to}
        backLabel={t(personalPageBackNav.more.labelKey)}
        backTestId="page-header-back-ownership-transfers"
      />

      {offlineBlocked ? (
        <OfflineNotice
          message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.PersonalOwnershipTransfer))}
        />
      ) : null}

      {actionError ? (
        <p
          className="m-0 text-[length:var(--exits-text-sm)] text-danger"
          role="alert"
          data-testid="ownership-transfer-action-error"
        >
          {actionError}
        </p>
      ) : null}

      {pendingQuery.isLoading ? <LoadingSkeleton count={3} /> : null}

      {pendingQuery.isError ? (
        <div className="flex flex-col gap-2">
          <ErrorState
            title={t("personal.ownershipTransfers.errorTitle")}
            detail={t("personal.ownershipTransfers.errorDetail")}
            error={pendingQuery.error}
            operation="list pending ownership transfers"
          />
          <Button
            type="button"
            variant="outline"
            data-testid="ownership-transfer-retry"
            onClick={() => void pendingQuery.refetch()}
          >
            {t("personal.ownershipTransfers.retry")}
          </Button>
        </div>
      ) : null}

      {!pendingQuery.isLoading && !pendingQuery.isError && (pendingQuery.data?.length ?? 0) === 0 ? (
        <div data-testid="ownership-transfer-empty">
          <EmptyState
            title={t("personal.ownershipTransfers.emptyTitle")}
            detail={t("personal.ownershipTransfers.emptyDetail")}
          />
        </div>
      ) : null}

      {!pendingQuery.isLoading && !pendingQuery.isError && (pendingQuery.data?.length ?? 0) > 0 ? (
        <ul className="m-0 flex list-none flex-col gap-3 p-0" role="list">
          {pendingQuery.data!.map((transfer) => {
            const expired = isTransferExpired(transfer, nowMs);
            const name = organizationLabel(transfer);
            const showingAccept =
              confirmForId === transfer.id && confirmMode === "accept";
            const showingDecline =
              confirmForId === transfer.id && confirmMode === "decline";
            const thisAcceptBusy =
              acceptMutation.isPending && acceptMutation.variables === transfer.id;
            const thisDeclineBusy =
              declineMutation.isPending && declineMutation.variables === transfer.id;

            return (
              <li
                key={transfer.id}
                className="catalog-form-section exits-animate-panel personal-section gap-3"
                data-testid="ownership-transfer-card"
              >
                <div className="flex min-w-0 items-start justify-between gap-2">
                  <div className="min-w-0 flex flex-col gap-1">
                    <div className="flex min-w-0 items-center gap-2">
                      <Building2 className="size-4 shrink-0 text-muted" aria-hidden />
                      <h2 className="catalog-form-section__title m-0 truncate">{name}</h2>
                    </div>
                    {transfer.publicOrganizationId ? (
                      <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                        {transfer.publicOrganizationId}
                      </p>
                    ) : null}
                    <p className="m-0 flex items-center gap-1 text-[length:var(--exits-text-sm)] text-muted">
                      <CalendarClock className="size-3.5 shrink-0" aria-hidden />
                      {t("personal.ownershipTransfers.expiresAt").replace(
                        "{date}",
                        formatShortDate(transfer.expiresAtUtc),
                      )}
                    </p>
                  </div>
                  {expired ? (
                    <StatusChip tone="info">{t("personal.ownershipTransfers.statusExpired")}</StatusChip>
                  ) : (
                    <StatusChip tone="warning">
                      {t("personal.ownershipTransfers.statusPending")}
                    </StatusChip>
                  )}
                </div>

                <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                  {t("personal.ownershipTransfers.cardHint")}
                </p>

                {expired ? (
                  <p
                    className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                    data-testid="ownership-transfer-expired"
                  >
                    {t("personal.ownershipTransfers.expiredDetail")}
                  </p>
                ) : (
                  <div className="flex flex-wrap gap-2">
                    <Button
                      type="button"
                      disabled={busy || offlineBlocked}
                      data-testid="ownership-transfer-accept"
                      onClick={() => openConfirm(transfer.id, "accept")}
                    >
                      {t("personal.ownershipTransfers.accept")}
                    </Button>
                    <Button
                      type="button"
                      variant="outline"
                      disabled={busy || offlineBlocked}
                      data-testid="ownership-transfer-decline"
                      onClick={() => openConfirm(transfer.id, "decline")}
                    >
                      {t("personal.ownershipTransfers.decline")}
                    </Button>
                  </div>
                )}

                {showingAccept ? (
                  <div
                    className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3"
                    data-testid="ownership-transfer-accept-confirm"
                  >
                    <p className="m-0 font-semibold">
                      {t("personal.ownershipTransfers.acceptTitle").replace("{name}", name)}
                    </p>
                    <ul className="m-0 list-disc space-y-1 pl-4 text-[length:var(--exits-text-sm)] text-muted">
                      <li>{t("personal.ownershipTransfers.acceptBulletOwner")}</li>
                      <li>{t("personal.ownershipTransfers.acceptBulletLeave")}</li>
                      <li>{t("personal.ownershipTransfers.acceptBulletDataStays")}</li>
                      <li>{t("personal.ownershipTransfers.acceptBulletNotTransferred")}</li>
                    </ul>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        disabled={busy}
                        onClick={closeConfirm}
                      >
                        {t("personal.ownershipTransfers.cancel")}
                      </Button>
                      <Button
                        type="button"
                        disabled={busy || offlineBlocked}
                        data-testid="ownership-transfer-accept-submit"
                        onClick={() => void onAcceptConfirm(transfer)}
                      >
                        {thisAcceptBusy ? (
                          <Loader2 className="size-4 animate-spin" aria-hidden />
                        ) : (
                          <Check className="size-4" aria-hidden />
                        )}
                        {t("personal.ownershipTransfers.acceptConfirm")}
                      </Button>
                    </div>
                  </div>
                ) : null}

                {showingDecline ? (
                  <div
                    className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3"
                    data-testid="ownership-transfer-decline-confirm"
                  >
                    <p className="m-0 text-[length:var(--exits-text-sm)]">
                      {t("personal.ownershipTransfers.declineTitle").replace("{name}", name)}
                    </p>
                    <div className="flex flex-wrap gap-2">
                      <Button
                        type="button"
                        variant="outline"
                        disabled={busy}
                        onClick={closeConfirm}
                      >
                        {t("personal.ownershipTransfers.cancel")}
                      </Button>
                      <Button
                        type="button"
                        disabled={busy || offlineBlocked}
                        data-testid="ownership-transfer-decline-submit"
                        onClick={() => void onDeclineConfirm(transfer)}
                      >
                        {thisDeclineBusy ? (
                          <Loader2 className="size-4 animate-spin" aria-hidden />
                        ) : (
                          <X className="size-4" aria-hidden />
                        )}
                        {t("personal.ownershipTransfers.declineConfirm")}
                      </Button>
                    </div>
                  </div>
                ) : null}
              </li>
            );
          })}
        </ul>
      ) : null}
    </div>
  );
}
