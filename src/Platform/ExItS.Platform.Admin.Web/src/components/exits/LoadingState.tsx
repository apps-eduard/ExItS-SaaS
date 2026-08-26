import type { ReactNode } from "react";
import { DashboardWidgetSkeleton } from "@/components/exits/dashboard/DashboardWidgetSkeleton";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

export function LoadingState({
  labelKey = "ui.loading",
  rows = 6,
  children,
}: {
  labelKey?: MessageKey;
  rows?: number;
  children?: ReactNode;
}) {
  const { t } = usePreferences();

  return (
    <section
      role="status"
      aria-busy="true"
      aria-label={t(labelKey)}
      className="grid gap-3"
    >
      {children ?? <DashboardWidgetSkeleton rows={rows} />}
    </section>
  );
}
