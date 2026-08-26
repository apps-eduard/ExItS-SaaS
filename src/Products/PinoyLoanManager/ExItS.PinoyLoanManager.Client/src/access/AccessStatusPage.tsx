import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { PageHeader } from "@/components/exits/PageHeader";
import { useI18n } from "@/i18n/I18nProvider";
import { useSession } from "@/session/SessionProvider";

export function AccessStatusPage({
  title,
  description,
  action,
}: {
  title: string;
  description: string;
  action?: { label: string; onClick: () => void };
}) {
  const { t } = useI18n();
  const { signOut } = useSession();

  return (
    <section className="mx-auto flex max-w-md flex-col gap-6 pt-6">
      <PageHeader title={title} description={description} />
      <Card className="flex flex-col gap-3">
        {action ? (
          <Button type="button" onClick={action.onClick}>
            {action.label}
          </Button>
        ) : null}
        <Button type="button" variant="ghost" onClick={() => void signOut()}>
          {t("auth.signOut")}
        </Button>
      </Card>
    </section>
  );
}
