import { ShieldOff } from "lucide-react";
import { PageHeader } from "@/components/exits/PageHeader";
import { usePreferences } from "@/hooks/use-preferences";

export function ForbiddenState({
  requiredPermission,
}: {
  requiredPermission?: string;
}) {
  const { t } = usePreferences();

  return (
    <section className="grid gap-4" data-testid="forbidden-state" role="alert">
      <PageHeader
        title={t("shell.forbidden.title")}
        description={t("shell.forbidden.body")}
      />
      <div className="flex items-start gap-3 rounded-[var(--exits-density-radius)] border border-destructive/40 bg-[var(--exits-danger-bg)] p-4">
        <ShieldOff className="mt-0.5 size-5 shrink-0 text-destructive" aria-hidden="true" />
        <div className="min-w-0 text-[length:var(--exits-text-sm)] text-foreground">
          <p className="font-semibold">{t("shell.forbidden.accessDenied")}</p>
          {requiredPermission ? (
            <p className="mt-1 break-all font-mono text-[length:var(--exits-text-xs)] text-muted">
              {requiredPermission}
            </p>
          ) : null}
        </div>
      </div>
    </section>
  );
}
