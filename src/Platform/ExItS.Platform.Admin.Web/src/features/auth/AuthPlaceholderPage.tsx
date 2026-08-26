import { Link } from "react-router-dom";
import { Card } from "@/components/ui/card";
import { usePreferences } from "@/hooks/use-preferences";
import type { MessageKey } from "@/lib/i18n/messages";

export function AuthPlaceholderPage({ titleKey }: { titleKey: MessageKey }) {
  const { t } = usePreferences();

  return (
    <Card>
      <h1 className="text-[length:var(--exits-text-xl)] font-bold">{t(titleKey)}</h1>
      <p className="mt-2 text-muted">{t("auth.placeholder.unavailable")}</p>
      <Link
        className="mt-4 inline-block text-primary underline-offset-4 hover:underline"
        to="/admin/login"
      >
        {t("auth.signIn")}
      </Link>
    </Card>
  );
}
