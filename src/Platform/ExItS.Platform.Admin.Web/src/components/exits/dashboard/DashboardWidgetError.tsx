import { Button } from "@/components/ui/button";
import { usePreferences } from "@/hooks/use-preferences";

export function DashboardWidgetError({ onRetry }: { onRetry: () => void }) {
  const { t } = usePreferences();

  return (
    <div
      role="alert"
      className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between"
    >
      <p className="min-w-0 text-[length:var(--exits-text-sm)] text-destructive break-words">
        {t("dashboard.widgetError")}
      </p>
      <Button type="button" size="sm" variant="outline" onClick={onRetry}>
        {t("diagnostics.retry")}
      </Button>
    </div>
  );
}
