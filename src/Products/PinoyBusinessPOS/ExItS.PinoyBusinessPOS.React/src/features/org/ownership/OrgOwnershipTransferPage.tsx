import { useState } from "react";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import { CalendarClock, KeyRound, Loader2, UserRound, X } from "lucide-react";
import {
  cancelOwnershipTransfer,
  getPendingOwnershipTransferForOrg,
  requestOwnershipTransfer,
  resolveOwnershipTransferTarget,
  type OrganizationOwnershipTransferDto,
  type OwnershipTransferTargetDto,
} from "@/api/platform/ownership-transfer-client";
import { PlatformApiError } from "@/api/platform/platform-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ErrorState } from "@/components/exits/ErrorState";
import { LoadingSkeleton } from "@/components/exits/FoundationStates";
import { PageHeader } from "@/components/exits/PageHeader";
import { StatusChip } from "@/components/exits/StatusChip";
import { useBrowserOnline } from "@/connectivity/browser-online";
import { formatShortDate } from "@/features/personal/people-status";
import { useI18n } from "@/i18n/I18nProvider";
import { pageBackNav } from "@/navigation/page-back-nav";
import { ONLINE_REQUIRED_CODES, onlineRequiredDetailKey } from "@/offline/online-required";
import { useWorkspace } from "@/workspace/WorkspaceProvider";

export function orgOwnershipTransferQueryKey(organizationId: string) {
  return ["org", "ownership-transfer", "pending", organizationId] as const;
}

function organizationLabel(
  transfer: OrganizationOwnershipTransferDto,
  fallbackName: string | undefined,
): string {
  return (
    transfer.organizationDisplayName?.trim() ||
    transfer.publicOrganizationId?.trim() ||
    fallbackName?.trim() ||
    transfer.organizationId
  );
}

function targetLabel(transfer: OrganizationOwnershipTransferDto): string {
  return transfer.toDisplayName?.trim() || transfer.toPublicUserId?.trim() || transfer.toUserId;
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

export function OrgOwnershipTransferPage() {
  const { t } = useI18n();
  const queryClient = useQueryClient();
  const online = useBrowserOnline();
  const { boundWorkspace } = useWorkspace();
  const organizationId = boundWorkspace?.organizationId ?? null;
  const orgDisplayName = boundWorkspace?.organizationDisplayName;

  const [targetInput, setTargetInput] = useState("");
  const [resolved, setResolved] = useState<OwnershipTransferTargetDto | null>(null);
  const [showRequestConfirm, setShowRequestConfirm] = useState(false);
  const [showCancelConfirm, setShowCancelConfirm] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  const pendingQuery = useQuery({
    queryKey: organizationId
      ? orgOwnershipTransferQueryKey(organizationId)
      : ["org", "ownership-transfer", "pending", "none"],
    enabled: organizationId !== null,
    queryFn: ({ signal }) => getPendingOwnershipTransferForOrg(organizationId!, signal),
  });

  const resolveMutation = useMutation({
    mutationFn: (input: string) => resolveOwnershipTransferTarget(organizationId!, input),
  });

  const requestMutation = useMutation({
    mutationFn: (input: string) => requestOwnershipTransfer(organizationId!, input),
  });

  const cancelMutation = useMutation({
    mutationFn: (transferId: string) => cancelOwnershipTransfer(transferId),
  });

  const busy =
    resolveMutation.isPending || requestMutation.isPending || cancelMutation.isPending;
  const offlineBlocked = !online;

  async function refetchPending() {
    if (!organizationId) return;
    await queryClient.invalidateQueries({
      queryKey: orgOwnershipTransferQueryKey(organizationId),
    });
  }

  function resetInitiate() {
    setResolved(null);
    setShowRequestConfirm(false);
    setTargetInput("");
  }

  async function onResolve() {
    if (!organizationId || busy || offlineBlocked) return;
    const trimmed = targetInput.trim();
    if (!trimmed) {
      setActionError(t("org.ownershipTransfer.targetRequired"));
      return;
    }
    setActionError(null);
    setShowRequestConfirm(false);
    try {
      const target = await resolveMutation.mutateAsync(trimmed);
      setResolved(target);
    } catch (error) {
      setResolved(null);
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("org.ownershipTransfer.resolveFailed"),
      );
    }
  }

  async function onRequestConfirm() {
    if (!organizationId || !resolved || busy || offlineBlocked) {
      if (offlineBlocked) {
        setActionError(t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.OrgOwnershipTransfer)));
      }
      return;
    }
    setActionError(null);
    try {
      await requestMutation.mutateAsync(resolved.publicUserId);
      resetInitiate();
      await refetchPending();
    } catch (error) {
      if (isConflictError(error)) {
        await refetchPending();
        setActionError(t("org.ownershipTransfer.staleConflict"));
        setShowRequestConfirm(false);
        return;
      }
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("org.ownershipTransfer.requestFailed"),
      );
    }
  }

  async function onCancelConfirm(transfer: OrganizationOwnershipTransferDto) {
    if (busy || offlineBlocked) {
      if (offlineBlocked) {
        setActionError(t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.OrgOwnershipTransfer)));
      }
      return;
    }
    setActionError(null);
    try {
      await cancelMutation.mutateAsync(transfer.id);
      setShowCancelConfirm(false);
      await refetchPending();
    } catch (error) {
      if (isConflictError(error)) {
        await refetchPending();
        setActionError(t("org.ownershipTransfer.staleConflict"));
        setShowCancelConfirm(false);
        return;
      }
      setActionError(
        error instanceof PlatformApiError
          ? error.message
          : t("org.ownershipTransfer.cancelFailed"),
      );
    }
  }

  if (!organizationId) {
    return (
      <div
        className="exits-page flex min-w-0 flex-col gap-3"
        data-testid="org-ownership-transfer-page"
      >
        <PageHeader
          title={t("org.ownershipTransfer.title")}
          description={t("org.ownershipTransfer.lede")}
          backTo={pageBackNav.org.to}
          backLabel={t(pageBackNav.org.labelKey)}
          backTestId="page-header-back-org"
        />
        <ErrorState
          title={t("org.ownershipTransfer.errorTitle")}
          detail={t("org.ownershipTransfer.noOrg")}
        />
      </div>
    );
  }

  const pending = pendingQuery.data ?? null;

  return (
    <div
      className="exits-page mx-auto flex w-full max-w-2xl min-w-0 flex-col gap-3"
      data-testid="org-ownership-transfer-page"
    >
      <PageHeader
        title={t("org.ownershipTransfer.title")}
        description={t("org.ownershipTransfer.lede")}
        backTo={pageBackNav.org.to}
        backLabel={t(pageBackNav.org.labelKey)}
        backTestId="page-header-back-org"
      />

      {offlineBlocked ? (
        <OfflineNotice
          message={t(onlineRequiredDetailKey(ONLINE_REQUIRED_CODES.OrgOwnershipTransfer))}
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
            title={t("org.ownershipTransfer.errorTitle")}
            detail={t("org.ownershipTransfer.errorDetail")}
            error={pendingQuery.error}
            operation="get pending ownership transfer"
          />
          <Button
            type="button"
            variant="outline"
            data-testid="ownership-transfer-retry"
            onClick={() => void pendingQuery.refetch()}
          >
            {t("org.ownershipTransfer.retry")}
          </Button>
        </div>
      ) : null}

      {!pendingQuery.isLoading && !pendingQuery.isError && pending ? (
        <Card
          className="flex flex-col gap-3"
          data-testid="ownership-pending-card"
        >
          <div className="flex min-w-0 items-start justify-between gap-2">
            <div className="min-w-0 flex flex-col gap-1">
              <div className="flex min-w-0 items-center gap-2">
                <KeyRound className="size-4 shrink-0 text-muted" aria-hidden />
                <h2 className="m-0 truncate text-[length:var(--exits-text-md)] font-semibold">
                  {organizationLabel(pending, orgDisplayName)}
                </h2>
              </div>
              <p className="m-0 flex min-w-0 items-center gap-1 text-[length:var(--exits-text-sm)] text-muted">
                <UserRound className="size-3.5 shrink-0" aria-hidden />
                <span className="truncate" data-testid="ownership-pending-target">
                  {[pending.toDisplayName?.trim(), pending.toPublicUserId?.trim()]
                    .filter(Boolean)
                    .join(" · ") || targetLabel(pending)}
                </span>
              </p>
              <p className="m-0 flex items-center gap-1 text-[length:var(--exits-text-sm)] text-muted">
                <CalendarClock className="size-3.5 shrink-0" aria-hidden />
                {t("org.ownershipTransfer.createdAt").replace(
                  "{date}",
                  formatShortDate(pending.createdAtUtc),
                )}
              </p>
              <p className="m-0 flex items-center gap-1 text-[length:var(--exits-text-sm)] text-muted">
                <CalendarClock className="size-3.5 shrink-0" aria-hidden />
                {t("org.ownershipTransfer.expiresAt").replace(
                  "{date}",
                  formatShortDate(pending.expiresAtUtc),
                )}
              </p>
            </div>
            <StatusChip tone="warning">
              {t("org.ownershipTransfer.statusPending")}
            </StatusChip>
          </div>

          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("org.ownershipTransfer.pendingHint")}
          </p>

          <div className="flex flex-wrap gap-2">
            <Button
              type="button"
              variant="outline"
              disabled={busy || offlineBlocked}
              data-testid="ownership-cancel"
              onClick={() => {
                if (busy || offlineBlocked) return;
                setActionError(null);
                setShowCancelConfirm(true);
              }}
            >
              {t("org.ownershipTransfer.cancel")}
            </Button>
          </div>

          {showCancelConfirm ? (
            <div
              className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3"
              data-testid="ownership-cancel-confirm"
            >
              <p className="m-0 font-semibold">
                {t("org.ownershipTransfer.cancelTitle").replace(
                  "{name}",
                  targetLabel(pending),
                )}
              </p>
              <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
                {t("org.ownershipTransfer.cancelDetail")}
              </p>
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="outline"
                  disabled={busy}
                  onClick={() => {
                    if (busy) return;
                    setShowCancelConfirm(false);
                  }}
                >
                  {t("org.ownershipTransfer.keepPending")}
                </Button>
                <Button
                  type="button"
                  disabled={busy || offlineBlocked}
                  data-testid="ownership-cancel-submit"
                  onClick={() => void onCancelConfirm(pending)}
                >
                  {cancelMutation.isPending ? (
                    <Loader2 className="size-4 animate-spin" aria-hidden />
                  ) : (
                    <X className="size-4" aria-hidden />
                  )}
                  {t("org.ownershipTransfer.cancelConfirm")}
                </Button>
              </div>
            </div>
          ) : null}
        </Card>
      ) : null}

      {!pendingQuery.isLoading && !pendingQuery.isError && !pending ? (
        <Card className="flex flex-col gap-3" data-testid="ownership-initiate-form">
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("org.ownershipTransfer.initiateHint")}
          </p>

          <Input
            label={t("org.ownershipTransfer.targetLabel")}
            name="ownership-target"
            value={targetInput}
            placeholder={t("org.ownershipTransfer.targetPlaceholder")}
            disabled={busy || offlineBlocked || Boolean(resolved)}
            data-testid="ownership-target-input"
            onChange={(event) => {
              setTargetInput(event.target.value);
              setActionError(null);
            }}
          />
          <p className="m-0 text-[length:var(--exits-text-sm)] text-muted">
            {t("org.ownershipTransfer.targetHint")}
          </p>

          {!resolved ? (
            <div className="flex flex-wrap gap-2">
              <Button
                type="button"
                disabled={busy || offlineBlocked || !targetInput.trim()}
                data-testid="ownership-resolve"
                onClick={() => void onResolve()}
              >
                {resolveMutation.isPending ? (
                  <Loader2 className="size-4 animate-spin" aria-hidden />
                ) : null}
                {t("org.ownershipTransfer.resolve")}
              </Button>
            </div>
          ) : (
            <div
              className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3"
              data-testid="ownership-resolved-target"
            >
              <p className="m-0 font-semibold">{t("org.ownershipTransfer.resolvedTitle")}</p>
              <p className="m-0 text-[length:var(--exits-text-sm)]" data-testid="ownership-resolved-name">
                {resolved.displayName}
              </p>
              <p
                className="m-0 text-[length:var(--exits-text-sm)] text-muted"
                data-testid="ownership-resolved-id"
              >
                {resolved.publicUserId}
              </p>
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="outline"
                  disabled={busy}
                  data-testid="ownership-change-target"
                  onClick={() => {
                    if (busy) return;
                    setResolved(null);
                    setShowRequestConfirm(false);
                  }}
                >
                  {t("org.ownershipTransfer.changeTarget")}
                </Button>
                <Button
                  type="button"
                  disabled={busy || offlineBlocked}
                  data-testid="ownership-request"
                  onClick={() => {
                    if (busy || offlineBlocked) return;
                    setActionError(null);
                    setShowRequestConfirm(true);
                  }}
                >
                  {t("org.ownershipTransfer.request")}
                </Button>
              </div>
            </div>
          )}

          {showRequestConfirm && resolved ? (
            <div
              className="flex flex-col gap-2 rounded-[var(--exits-radius-md)] border border-border bg-surface p-3"
              data-testid="ownership-request-confirm"
            >
              <p className="m-0 font-semibold">
                {t("org.ownershipTransfer.requestTitle").replace(
                  "{name}",
                  resolved.displayName,
                )}
              </p>
              <ul className="m-0 list-disc space-y-1 pl-4 text-[length:var(--exits-text-sm)] text-muted">
                <li>{t("org.ownershipTransfer.confirmBulletRecipientAccept")}</li>
                <li>{t("org.ownershipTransfer.confirmBulletExpires")}</li>
                <li>{t("org.ownershipTransfer.confirmBulletOwner")}</li>
                <li>{t("org.ownershipTransfer.confirmBulletLeave")}</li>
                <li>{t("org.ownershipTransfer.confirmBulletDataStays")}</li>
                <li>{t("org.ownershipTransfer.confirmBulletNotTransferred")}</li>
              </ul>
              <div className="flex flex-wrap gap-2">
                <Button
                  type="button"
                  variant="outline"
                  disabled={busy}
                  onClick={() => {
                    if (busy) return;
                    setShowRequestConfirm(false);
                  }}
                >
                  {t("org.ownershipTransfer.back")}
                </Button>
                <Button
                  type="button"
                  disabled={busy || offlineBlocked}
                  data-testid="ownership-request-submit"
                  onClick={() => void onRequestConfirm()}
                >
                  {requestMutation.isPending ? (
                    <Loader2 className="size-4 animate-spin" aria-hidden />
                  ) : (
                    <KeyRound className="size-4" aria-hidden />
                  )}
                  {t("org.ownershipTransfer.requestConfirm")}
                </Button>
              </div>
            </div>
          ) : null}
        </Card>
      ) : null}
    </div>
  );
}
