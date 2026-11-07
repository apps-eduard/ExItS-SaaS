import { RefreshCw } from "lucide-react";
import { useTranslation } from "react-i18next";
import type { HomeWidget, WidgetStatus } from "@/api/widget";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";

function WidgetMessage({
  status,
  message,
  onRetry,
}: {
  status: WidgetStatus;
  message?: string;
  onRetry?: () => void;
}) {
  const { t } = useTranslation();
  if (status === "loading") {
    return (
      <div className="space-y-2">
        <Skeleton className="h-3 w-1/2" />
        <Skeleton className="h-6 w-3/4" />
      </div>
    );
  }
  const label =
    status === "forbidden"
      ? t("homeData.noPermission")
      : status === "unavailable"
        ? t("homeData.unavailable")
        : t("homeData.loadError");
  return (
    <div className="space-y-2">
      <p className="text-xs font-medium text-muted-foreground">{label}</p>
      {message ? (
        <p className="text-[11px] leading-snug text-muted-foreground/90">
          {message}
        </p>
      ) : null}
      {onRetry && (status === "error" || status === "unauthorized") ? (
        <Button type="button" size="sm" variant="outline" onClick={onRetry}>
          <RefreshCw className="size-3.5" aria-hidden />
          {t("homeData.retry")}
        </Button>
      ) : null}
    </div>
  );
}

function SectionLabel({ children }: { children: React.ReactNode }) {
  return (
    <h2 className="text-[11px] font-semibold uppercase tracking-[0.12em] text-muted-foreground">
      {children}
    </h2>
  );
}

function isFailedWidget(widget: HomeWidget<unknown>): boolean {
  return (
    widget.status === "error" ||
    widget.status === "unauthorized" ||
    widget.status === "forbidden" ||
    widget.status === "unavailable"
  );
}

export { WidgetMessage, SectionLabel, isFailedWidget };
