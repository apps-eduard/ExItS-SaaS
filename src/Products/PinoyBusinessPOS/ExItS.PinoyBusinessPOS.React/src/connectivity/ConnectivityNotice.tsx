import { useI18n } from "@/i18n/I18nProvider";

export function ConnectivityNotice({
  offline,
  reconnecting = false,
  backOnline = false,
}: {
  offline: boolean;
  reconnecting?: boolean;
  backOnline?: boolean;
}) {
  const { t } = useI18n();

  if (backOnline) {
    return (
      <div
        className="pointer-events-none fixed inset-x-0 top-[max(0.75rem,env(safe-area-inset-top))] z-[1200] flex justify-center px-4"
        role="status"
        aria-live="polite"
        aria-atomic="true"
        data-testid="connectivity-back-online"
      >
        <div className="pointer-events-auto max-w-sm min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-center shadow-sm">
          <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold text-success">
            {t("connectivity.backOnline")}
          </p>
        </div>
      </div>
    );
  }

  if (!offline) {
    return null;
  }

  return (
    <div
      className="pointer-events-none fixed inset-x-0 top-[max(0.75rem,env(safe-area-inset-top))] z-[1200] flex justify-center px-4"
      role="status"
      aria-live="polite"
      aria-atomic="true"
      data-testid="connectivity-notice"
    >
      <div className="pointer-events-auto max-w-sm min-w-0 rounded-[var(--exits-radius-md)] border border-border bg-surface px-3 py-2 text-center shadow-sm">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("connectivity.offlineTitle")}
        </p>
        <p className="m-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {reconnecting ? t("connectivity.reconnecting") : t("connectivity.staleDataDetail")}
        </p>
      </div>
    </div>
  );
}
