import { resolveMailpitConvenienceUrl } from "@/lib/auth/mailpit-url";
import { isLocalValidationToolsEnabled } from "@/lib/env";
import { usePreferences } from "@/hooks/use-preferences";

export function MailpitConvenienceHint() {
  const { t } = usePreferences();

  if (!isLocalValidationToolsEnabled()) {
    return null;
  }

  const href = resolveMailpitConvenienceUrl();
  if (!href) {
    return null;
  }

  return (
    <p className="mt-3 text-[length:var(--exits-text-sm)] text-muted">
      {t("auth.mailpit.hint")}{" "}
      <a
        className="text-primary underline-offset-4 hover:underline"
        href={href}
        rel="noreferrer"
        target="_blank"
      >
        {t("auth.mailpit.open")}
      </a>
    </p>
  );
}
