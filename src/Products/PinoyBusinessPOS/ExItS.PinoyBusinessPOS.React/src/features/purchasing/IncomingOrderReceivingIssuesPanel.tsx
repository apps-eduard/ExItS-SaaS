import { useMemo, useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  resolveIncomingOrderReceivingIssue,
  type ConnectedPoReceivingIssue,
  type ConnectedPoReceivingIssueLine,
} from "@/api/pos/pos-connected-suppliers-client";
import type { PosWorkspaceScope } from "@/api/pos/pos-http";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Notice } from "@/components/exits/Notice";
import { StatusChip } from "@/components/exits/StatusChip";
import { useExitsToast } from "@/components/exits/ToastProvider";
import { describePosApiError } from "@/access/pos-commercial-errors";
import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { formatQuantityValue } from "@/lib/quantity-rules";

const MISSING_RESOLUTIONS = [
  "FoundAtSeller",
  "NeverShipped",
  "LostInTransit",
  "DeliveredDisputed",
  "ReplacementPlanned",
  "Other",
] as const;

const DAMAGED_RESOLUTIONS = [
  "AcceptedNoReturn",
  "ReturnRequested",
  "ReplacementApproved",
  "Disputed",
  "Other",
] as const;

function missingPreview(resolution: string, qty: number, t: (k: MessageKey) => string): string {
  if (resolution === "FoundAtSeller" || resolution === "NeverShipped") {
    return t("incomingOrders.receivingIssues.previewRestore").replace(
      "{qty}",
      formatQuantityValue(qty),
    );
  }
  return t("incomingOrders.receivingIssues.previewNone");
}

function damagedPreview(resolution: string, t: (k: MessageKey) => string): string {
  if (resolution === "ReturnRequested") {
    return t("incomingOrders.receivingIssues.previewReturn");
  }
  return t("incomingOrders.receivingIssues.previewNone");
}

type Props = {
  workspace: PosWorkspaceScope;
  orderId: string;
  issues: ConnectedPoReceivingIssue[];
  canManage: boolean;
};

export function IncomingOrderReceivingIssuesPanel({ workspace, orderId, issues, canManage }: Props) {
  const { t } = useI18n();
  const toast = useExitsToast();
  const queryClient = useQueryClient();
  const [activeIssueId, setActiveIssueId] = useState<string | null>(null);
  const [drafts, setDrafts] = useState<Record<string, { resolution: string; note: string }>>({});

  const unresolved = useMemo(
    () => issues.filter((i) => i.status === "PendingSellerReview" || i.unresolvedLineCount > 0),
    [issues],
  );

  const resolveMutation = useMutation({
    mutationFn: async (issue: ConnectedPoReceivingIssue) => {
      const lines = issue.lines
        .filter((l) => !l.isResolved)
        .map((line) => {
          const draft = drafts[line.receivingIssueLineId];
          if (!draft?.resolution) {
            throw new Error(t("incomingOrders.receivingIssues.resolutionRequired"));
          }
          if (draft.resolution === "Other" && !draft.note.trim()) {
            throw new Error(t("incomingOrders.receivingIssues.noteRequired"));
          }
          return {
            receivingIssueLineId: line.receivingIssueLineId,
            missingResolution: line.lineKind === "Missing" ? draft.resolution : null,
            damagedResolution: line.lineKind === "Damaged" ? draft.resolution : null,
            resolutionQty: line.lineKind === "Missing" ? line.missingQty : line.damagedQty,
            sellerNote: draft.note.trim() || null,
          };
        });
      return resolveIncomingOrderReceivingIssue(workspace, orderId, issue.receivingIssueId, { lines });
    },
    onSuccess: async () => {
      toast.success(t("incomingOrders.receivingIssues.resolveSuccess"));
      setActiveIssueId(null);
      await queryClient.invalidateQueries({
        queryKey: ["connected-suppliers", "incoming-order", orderId],
      });
    },
    onError: (err) => {
      toast.error(describePosApiError(err, t("incomingOrders.actionFailed")));
    },
  });

  if (issues.length === 0) {
    return null;
  }

  return (
    <Card className="space-y-3 p-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <div>
          <h3 className="text-base font-semibold">{t("incomingOrders.receivingIssues.title")}</h3>
          <p className="text-sm text-muted-foreground">
            {unresolved.length > 0
              ? t("incomingOrders.receivingIssues.pendingReview")
              : t("incomingOrders.receivingIssues.allResolved")}
          </p>
        </div>
        <StatusChip tone={unresolved.length > 0 ? "warning" : "success"}>
          {unresolved.length > 0
            ? t("incomingOrders.receivingIssues.statusPending")
            : t("incomingOrders.receivingIssues.statusResolved")}
        </StatusChip>
      </div>

      {issues.map((issue) => {
        const reviewing = activeIssueId === issue.receivingIssueId;
        return (
          <div key={issue.receivingIssueId} className="space-y-3 rounded-md border border-border p-3">
            <div className="flex flex-wrap items-center justify-between gap-2">
              <div className="text-sm text-muted-foreground">
                {t("incomingOrders.receivingIssues.unresolvedCount").replace(
                  "{count}",
                  String(issue.unresolvedLineCount),
                )}
              </div>
              {canManage && issue.unresolvedLineCount > 0 ? (
                <Button
                  type="button"
                  variant={reviewing ? "secondary" : "default"}
                  size="sm"
                  onClick={() => setActiveIssueId(reviewing ? null : issue.receivingIssueId)}
                >
                  {reviewing
                    ? t("incomingOrders.receivingIssues.hideReview")
                    : t("incomingOrders.receivingIssues.review")}
                </Button>
              ) : null}
            </div>

            <ul className="space-y-3">
              {issue.lines.map((line) => (
                <ReceivingIssueLineRow
                  key={line.receivingIssueLineId}
                  line={line}
                  reviewing={reviewing && !line.isResolved}
                  draft={drafts[line.receivingIssueLineId]}
                  onDraftChange={(next) =>
                    setDrafts((prev) => ({ ...prev, [line.receivingIssueLineId]: next }))
                  }
                  t={t}
                />
              ))}
            </ul>

            {reviewing && canManage ? (
              <div className="flex justify-end gap-2">
                <Button
                  type="button"
                  variant="ghost"
                  onClick={() => setActiveIssueId(null)}
                  disabled={resolveMutation.isPending}
                >
                  {t("incomingOrders.receivingIssues.cancel")}
                </Button>
                <Button
                  type="button"
                  onClick={() => resolveMutation.mutate(issue)}
                  disabled={resolveMutation.isPending}
                >
                  {t("incomingOrders.receivingIssues.confirm")}
                </Button>
              </div>
            ) : null}
          </div>
        );
      })}
    </Card>
  );
}

function ReceivingIssueLineRow({
  line,
  reviewing,
  draft,
  onDraftChange,
  t,
}: {
  line: ConnectedPoReceivingIssueLine;
  reviewing: boolean;
  draft?: { resolution: string; note: string };
  onDraftChange: (next: { resolution: string; note: string }) => void;
  t: (key: MessageKey) => string;
}) {
  const options = line.lineKind === "Missing" ? MISSING_RESOLUTIONS : DAMAGED_RESOLUTIONS;
  const resolution = draft?.resolution ?? "";
  const preview =
    resolution.length === 0
      ? null
      : line.lineKind === "Missing"
        ? missingPreview(resolution, line.missingQty, t)
        : damagedPreview(resolution, t);

  return (
    <li className="space-y-2 rounded-md bg-muted/30 p-3 text-sm">
      <div className="font-medium">{line.nameSnapshot}</div>
      <div className="grid gap-1 sm:grid-cols-2 lg:grid-cols-4">
        <span>
          {t("incomingOrders.receivingIssues.shipped")}: {formatQuantityValue(line.shippedQty)}
        </span>
        <span>
          {t("incomingOrders.receivingIssues.good")}: {formatQuantityValue(line.goodQty)}
        </span>
        <span>
          {t("incomingOrders.receivingIssues.damaged")}: {formatQuantityValue(line.damagedQty)}
        </span>
        <span>
          {t("incomingOrders.receivingIssues.missing")}: {formatQuantityValue(line.missingQty)}
        </span>
      </div>
      {line.buyerDiscrepancyNote ? (
        <p className="text-muted-foreground">
          {t("incomingOrders.receivingIssues.buyerNote")}: {line.buyerDiscrepancyNote}
        </p>
      ) : null}
      {line.isResolved ? (
        <Notice tone="success">
          {line.lineKind === "Missing" ? line.missingResolution : line.damagedResolution}
          {line.sellerNote ? ` — ${line.sellerNote}` : ""}
          {line.inventoryMovementId
            ? ` · ${t("incomingOrders.receivingIssues.stockRestored")}`
            : ` · ${t("incomingOrders.receivingIssues.previewNone")}`}
        </Notice>
      ) : reviewing ? (
        <div className="space-y-2">
          <label className="block text-xs font-medium">
            {t("incomingOrders.receivingIssues.chooseResolution")}
            <select
              className="mt-1 w-full rounded-md border border-border bg-background px-2 py-1.5"
              value={resolution}
              onChange={(e) =>
                onDraftChange({ resolution: e.target.value, note: draft?.note ?? "" })
              }
            >
              <option value="">{t("incomingOrders.receivingIssues.selectPlaceholder")}</option>
              {options.map((code) => (
                <option key={code} value={code}>
                  {t(`incomingOrders.receivingIssues.resolution.${code}` as MessageKey)}
                </option>
              ))}
            </select>
          </label>
          {preview ? <Notice tone="info">{preview}</Notice> : null}
          <label className="block text-xs font-medium">
            {t("incomingOrders.receivingIssues.sellerNote")}
            <textarea
              className="mt-1 w-full rounded-md border border-border bg-background px-2 py-1.5"
              rows={2}
              value={draft?.note ?? ""}
              onChange={(e) =>
                onDraftChange({ resolution: draft?.resolution ?? "", note: e.target.value })
              }
            />
          </label>
        </div>
      ) : (
        <StatusChip tone="warning">{t("incomingOrders.receivingIssues.statusPending")}</StatusChip>
      )}
    </li>
  );
}
