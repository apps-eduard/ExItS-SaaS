import { useI18n } from "@/i18n/I18nProvider";
import type { MessageKey } from "@/i18n/messages";
import { cn } from "@/lib/cn";
import type { OrganizationActorDisplayName } from "@/api/platform/actor-directory-client";

export type ActorAttributionProps = {
  labelKey: MessageKey;
  actorId?: string | null;
  occurredAtUtc?: string | null;
  /** When true, show System instead of resolving a user. */
  isSystem?: boolean;
  resolved?: OrganizationActorDisplayName | null;
  isLoading?: boolean;
  className?: string;
  /** Hide the timestamp row even when occurredAtUtc is set. */
  hideTimestamp?: boolean;
  testId?: string;
};

function formatActorWhen(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) {
    return iso;
  }
  return date.toLocaleString();
}

/**
 * Secondary provenance line for internal detail/history surfaces.
 * Never renders raw actor GUIDs in normal UI.
 */
export function ActorAttribution({
  labelKey,
  actorId,
  occurredAtUtc,
  isSystem = false,
  resolved,
  isLoading = false,
  className,
  hideTimestamp = false,
  testId,
}: ActorAttributionProps) {
  const { t } = useI18n();

  if (!isSystem && !actorId && !occurredAtUtc) {
    return null;
  }

  let name: string;
  let statusHint: string | null = null;

  if (isSystem) {
    name = t("common.system");
  } else if (!actorId) {
    name = t("common.notAvailable");
  } else if (isLoading && !resolved) {
    name = "\u00a0";
  } else if (resolved?.actorStatus === "NotAvailable" || !resolved?.displayName) {
    name = t("common.notAvailable");
  } else {
    name = resolved.displayName;
    if (resolved.actorStatus === "FormerStaff") {
      statusHint = t("common.formerStaff");
    }
  }

  return (
    <div
      className={cn(
        "min-h-[2.75rem] text-[length:var(--exits-text-sm)] text-muted",
        className,
      )}
      data-testid={testId ?? "actor-attribution"}
    >
      <p className="m-0 text-[length:var(--exits-text-xs)] uppercase tracking-wide opacity-80">
        {t(labelKey)}
      </p>
      <p
        className={cn(
          "m-0 font-medium text-foreground",
          isLoading && !resolved && !isSystem && "animate-pulse bg-muted/40 text-transparent",
        )}
        data-testid="actor-attribution-name"
      >
        {name}
      </p>
      {statusHint ? (
        <p className="m-0 text-[length:var(--exits-text-xs)]" data-testid="actor-attribution-status">
          {statusHint}
        </p>
      ) : null}
      {!hideTimestamp && occurredAtUtc ? (
        <p className="m-0 text-[length:var(--exits-text-xs)]" data-testid="actor-attribution-when">
          {formatActorWhen(occurredAtUtc)}
        </p>
      ) : null}
    </div>
  );
}

export type ActorNameProps = {
  actorId?: string | null;
  resolved?: OrganizationActorDisplayName | null;
  isLoading?: boolean;
  isSystem?: boolean;
  className?: string;
};

/** Inline actor name only (for timelines). */
export function ActorName({
  actorId,
  resolved,
  isLoading = false,
  isSystem = false,
  className,
}: ActorNameProps) {
  const { t } = useI18n();

  if (isSystem) {
    return <span className={className}>{t("common.system")}</span>;
  }
  if (!actorId) {
    return null;
  }
  if (isLoading && !resolved) {
    return (
      <span
        className={cn("inline-block min-w-[6rem] animate-pulse rounded bg-muted/40", className)}
        aria-hidden
      >
        &nbsp;
      </span>
    );
  }
  if (!resolved || resolved.actorStatus === "NotAvailable" || !resolved.displayName) {
    return <span className={className}>{t("common.notAvailable")}</span>;
  }
  return (
    <span className={className}>
      {resolved.displayName}
      {resolved.actorStatus === "FormerStaff" ? (
        <span className="ml-1 text-muted">({t("common.formerStaff")})</span>
      ) : null}
    </span>
  );
}
