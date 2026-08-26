import { ShieldOff } from "lucide-react";
import { PageHeader } from "@/components/exits/PageHeader";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

export function ForbiddenState({
  titleKey = "shell.forbidden.title",
  descriptionKey = "shell.forbidden.body",
  requiredPermission,
}: {
  titleKey?: MessageKey;
  descriptionKey?: MessageKey;
  requiredPermission?: string;
}) {
  const { t } = usePreferences();

  return (
    <section data-state="forbidden" className="grid gap-3">
      <PageHeader
        title={t(titleKey)}
        description={
          requiredPermission
            ? `${t(descriptionKey)} (${requiredPermission})`
            : t(descriptionKey)
        }
      />
      <div className="flex items-center gap-2 text-[length:var(--exits-text-sm)] text-muted">
        <ShieldOff aria-hidden="true" className="size-4 shrink-0" />
        <span>{t("shell.forbidden.hint")}</span>
      </div>
    </section>
  );
}
