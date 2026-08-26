import { WifiOff } from "lucide-react";
import { Card } from "@/components/ui/card";
import { useI18n } from "@/i18n/I18nProvider";
import { cn } from "@/lib/cn";
import { onlineRequiredDetailKey, type OnlineRequiredCode } from "@/offline/online-required";

/** Honest offline notice: names the one capability that needs the server, not a generic error. */
export function OnlineRequiredCard({
  code,
  className,
  testId = "online-required",
}: {
  code: OnlineRequiredCode;
  className?: string;
  testId?: string;
}) {
  const { t } = useI18n();

  return (
    <Card data-testid={testId} className={cn("flex items-start gap-3", className)}>
      <WifiOff className="mt-0.5 size-5 shrink-0 text-muted" aria-hidden />
      <div className="min-w-0">
        <p className="m-0 text-[length:var(--exits-text-sm)] font-semibold">
          {t("offline.internetRequiredTitle")}
        </p>
        <p className="mb-0 mt-1 text-[length:var(--exits-text-sm)] text-muted">
          {t(onlineRequiredDetailKey(code))}
        </p>
      </div>
    </Card>
  );
}
